using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine.SceneManagement;

public class AppointmentChecker : MonoBehaviour
{
    private string[] initialUrls = new string[]
    {
        "https://termine.staedteregion-aachen.de/auslaenderamt/",
        "https://termine.staedteregion-aachen.de/auslaenderamt/select2?md=1"
    };

    private string[] checkUrls = new string[]
    {
        "https://termine.staedteregion-aachen.de/auslaenderamt/location?mdt=89&select_cnc=1&cnc-299=0&cnc-300=0&cnc-293=2&cnc-296=0&cnc-297=0&cnc-301=0&cnc-284=0&cnc-298=0&cnc-291=0&cnc-285=0&cnc-282=0&cnc-283=0&cnc-303=0&cnc-281=0&cnc-287=0&cnc-286=0&cnc-289=0&cnc-292=0&cnc-288=0&cnc-279=0&cnc-280=0&cnc-290=0&cnc-295=0&cnc-294=0",
        "https://termine.staedteregion-aachen.de/auslaenderamt/location?mdt=89&select_cnc=1&cnc-299=0&cnc-300=0&cnc-293=0&cnc-296=2&cnc-297=0&cnc-301=0&cnc-284=0&cnc-298=0&cnc-291=0&cnc-285=0&cnc-282=0&cnc-283=0&cnc-303=0&cnc-281=0&cnc-287=0&cnc-286=0&cnc-289=0&cnc-292=0&cnc-288=0&cnc-279=0&cnc-280=0&cnc-290=0&cnc-295=0&cnc-294=0",
        "https://termine.staedteregion-aachen.de/auslaenderamt/location?mdt=89&select_cnc=1&cnc-299=0&cnc-300=0&cnc-293=0&cnc-296=0&cnc-297=2&cnc-301=0&cnc-284=0&cnc-298=0&cnc-291=0&cnc-285=0&cnc-282=0&cnc-283=0&cnc-303=0&cnc-281=0&cnc-287=0&cnc-286=0&cnc-289=0&cnc-292=0&cnc-288=0&cnc-279=0&cnc-280=0&cnc-290=0&cnc-295=0&cnc-294=0",
        "https://termine.staedteregion-aachen.de/auslaenderamt/location?mdt=94&select_cnc=1&cnc-299=0&cnc-300=0&cnc-293=0&cnc-296=0&cnc-297=0&cnc-313=0&cnc-284=0&cnc-315=0&cnc-312=0&cnc-317=0&cnc-310=1&cnc-283=0&cnc-303=0&cnc-309=0&cnc-287=0&cnc-286=0&cnc-289=0&cnc-292=0&cnc-288=0&cnc-279=0&cnc-280=0&cnc-311=0&cnc-295=0&cnc-327=0"
    };

    private string[] labels = new string[]
    {
        "Team 1", "Team 2", "Team 3", "Test"
    };

    public Text timestampText;
    public Text team1Text;
    public Text team2Text;
    public Text team3Text;
    public Text testText;

    private Text[] resultTexts;
    private DateTime[] earliestAppointments;

    private bool[] appointmentAvailable;

    void Start()
    {
        resultTexts = new Text[] { team1Text, team2Text, team3Text, testText };
        earliestAppointments = new DateTime[resultTexts.Length];
        appointmentAvailable = new bool[resultTexts.Length];

        if (resultTexts.Length != checkUrls.Length)
        {
            Debug.LogError("Bitte stellen Sie sicher, dass die Anzahl der resultTexts mit der Anzahl der checkUrls übereinstimmt.");
            return;
        }

        // Set initial text for all text elements
        for (int i = 0; i < resultTexts.Length; i++)
        {
            resultTexts[i].text = "Lade Daten...";
            earliestAppointments[i] = DateTime.MaxValue; // Initialisiere mit einem sehr späten Datum
            appointmentAvailable[i] = false;
        }

        StartCoroutine(CheckUrls());
        StartCoroutine(UpdateTime());
    }

    IEnumerator CheckUrls()
    {
        foreach (var url in initialUrls)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("Fehler beim Abrufen der URL: " + url + " Fehler: " + webRequest.error);
                }
                else
                {
                    Debug.Log("Seite geladen: " + url);
                }

                yield return new WaitForSeconds(1);
            }
        }

        StartCoroutine(CheckUrlsSimultaneously());
    }

    IEnumerator CheckUrlsSimultaneously()
    {
        while (true)
        {
            for (int i = 0; i < checkUrls.Length; i++)
            {
                StartCoroutine(CheckUrl(checkUrls[i], resultTexts[i], labels[i], i));
            }

            yield return new WaitForSeconds(60); // Setze den Intervall auf 60 Sekunden
        }
    }

    IEnumerator CheckUrl(string url, Text resultText, string label, int index)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Fehler beim Abrufen der URL: " + url + " Fehler: " + webRequest.error);
                resultText.text = label + ": Fehler beim Abrufen der Daten";
                yield break;
            }

            string responseText = webRequest.downloadHandler.text;
            if (responseText.Contains("Nächster Termin"))
            {
                string pattern = @"<dt>Nächster Termin<\/dt>\s*<dd>ab\s*(\d{2}\.\d{2}\.\d{4},\s*\d{2}:\d{2}\s*Uhr)<\/dd>";
                Match match = Regex.Match(responseText, pattern);
                if (match.Success)
                {
                    string dateTime = match.Groups[1].Value;
                    DateTime parsedDateTime;
                    if (DateTime.TryParse(dateTime, out parsedDateTime))
                    {
                        earliestAppointments[index] = parsedDateTime;
                        resultText.text = label + ": Termin verfügbar: " + dateTime;
                        appointmentAvailable[index] = true;
                    }
                    else
                    {
                        resultText.text = label + ": Termin verfügbar, aber Datum/Uhrzeit konnte nicht geparst werden";
                        appointmentAvailable[index] = false;
                    }
                }
                else
                {
                    resultText.text = label + ": Termin verfügbar, aber Datum/Uhrzeit nicht gefunden";
                    appointmentAvailable[index] = false;
                }
            }
            else
            {
                resultText.text = label + ": Kein Termin verfügbar";
                earliestAppointments[index] = DateTime.MaxValue; // Zurücksetzen, damit ein neuer Termin gefunden wird
                appointmentAvailable[index] = false;
            }

            if (appointmentAvailable[index] && index < 3)
            {
                resultText.color = Color.green; // Ändere die Textfarbe zu Gelb
                StartCoroutine(ScaleText(resultText));
            }

            Debug.Log(resultText.text + " auf: " + url);
        }
    }

    IEnumerator ScaleText(Text text)
    {
        Vector3 originalScale = text.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        while (true)
        {
            yield return ScaleCoroutine(text, originalScale, targetScale, 0.5f);
            yield return ScaleCoroutine(text, targetScale, originalScale, 0.5f);
        }
    }

    IEnumerator ScaleCoroutine(Text text, Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            text.transform.localScale = Vector3.Lerp(fromScale, toScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        text.transform.localScale = toScale;
    }

    IEnumerator UpdateTime()
    {
        while (true)
        {
            timestampText.text = DateTime.Now.ToString("dd.MM.yyyy, HH:mm:ss");
            yield return new WaitForSeconds(1);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Application.runInBackground = true;
        }
    }
    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Lädt die aktuelle Szene neu
    }
}
