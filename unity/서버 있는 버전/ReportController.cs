using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


[System.Serializable]
public class Report
{
    public int userIdx;
    public int expIdx;
    public int repScore;
    public List<string> repAnswers;
}

public class CheckController
{
    public string userId;
    public string refresh;
}


public class ReportController : MonoBehaviour
{
    [Header("reports")]
    public List<int> imageIdxs;
    public int userIdx;
    public GameObject other;
    private string token;
    public GameObject quizPanel;
    public GameObject ExpProcessUI;
    public GameObject QuizUI;


    private void Awake()
    {
        userIdx = PlayerPrefs.GetInt("userIdx");
        // userIdx = 1;
        token = PlayerPrefs.GetString("access");
        // token = "Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJoc21rMDA3NiIsInJvbGVzIjpbIlJPTEVfVVNFUiJdLCJpYXQiOjE2Njg2ODY2MTEsImV4cCI6MTY2ODY4NjkxMX0.rZxFYIIyyz1ey1jlSIEhk0WJy0uKFdrRMU396GZ2sF4";
    }


    public void WhenClicked()
    {
        other.SetActive(true);
        other.transform.Find("Report").gameObject.GetComponent<ReportSettingScript>().SaveUserReport();

    }

    public void SaveReport(Report report, List<int> selected)
    {
        string url = "https://k7d101.p.ssafy.io/api/report/save";
        // string url = "http://localhost:8000/api/report/save";
        string json = JsonConvert.SerializeObject(report);
        imageIdxs = selected;
        StartCoroutine(SaveRepCo(url, json));
    }

    public void SavePicture(int repIdx)
    {

        string url = $"https://k7d101.p.ssafy.io/api/report/picture/{userIdx}/{repIdx}/url";
        // string url = $"http://localhost:8000/api/report/picture/{userIdx}/{repIdx}/url";
        string json = JsonConvert.SerializeObject(imageIdxs);
        StartCoroutine(SavePicCo(url, json));
    }

    IEnumerator CheckRefreshController(string url, string json, int t)
    {
        CheckView ck = new CheckView
        {
            userId = PlayerPrefs.GetString("userId"),
            refresh = PlayerPrefs.GetString("refresh")
        };
        string checkJson = JsonConvert.SerializeObject(ck);
        using (UnityWebRequest request = UnityWebRequest.Post("https://k7d101.p.ssafy.io/api/user/check", checkJson))
        {
            byte[] checkJsonToSend = new System.Text.UTF8Encoding().GetBytes(checkJson);
            request.uploadHandler = new UploadHandlerRaw(checkJsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();


            // refresh 만료시
            if (request.responseCode == 403)
            {
                PlayerPrefs.DeleteAll();
                SceneManager.LoadSceneAsync("children_room");
            }

            // 해당 유저의 토큰이 아닐 때
            else if (request.responseCode == 400)
            {
                Debug.Log(request.downloadHandler.text);
            }

            // 성공 (Access 재발급)
            else if (request.responseCode == 200)
            {
                JObject obj = JObject.Parse(request.downloadHandler.text);
                token = "Bearer " + (string)obj["access"];
                PlayerPrefs.SetString("access", token);
                
                // 리포트 저장
                if (t == 1)
                {
                    StartCoroutine(SaveRepCo(url, json));
                }

                // 사진 저장
                if (t == 2)
                {
                    StartCoroutine(SavePicCo(url, json));
                }
            }
        }
    }

    IEnumerator SaveRepCo(string url, string json)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(url, json))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            // request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            // 오류
            if (request.responseCode == 404)
            {
                Debug.Log(request.downloadHandler.text);
            }

            // access 만료
            else if (request.responseCode == 403)
            {
                StartCoroutine(CheckRefreshController(url, json, 1));
            }

            // 해당 유저의 토큰이 아닐 때
            else if (request.responseCode == 400)
            {
                Debug.Log(request.downloadHandler.text);
            }

            // 성공
            else if (request.responseCode == 200)
            {
                other.SetActive(false);
                int repIdx = JObject.Parse(request.downloadHandler.text)["repIdx"].ToObject<int>();
                PlayerPrefs.SetInt("repIdx", repIdx);
                if (imageIdxs.Count != 0)
                {
                    SavePicture(repIdx);
                }

                ExpProcessUI.SetActive(false);
                quizPanel.SetActive(true);
                QuizUI.SetActive(true);
            }

            // 그 외
            else
            {
                Debug.Log(request.downloadHandler.text);
            }
            request.Dispose();
        }
    }

    IEnumerator SavePicCo(string url, string json)
    {
        using (UnityWebRequest request = UnityWebRequest.Post(url, json))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            // request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

            // 오류
            if (request.responseCode == 404)
            {
                Debug.Log(request.downloadHandler.text);
            }

            // access 만료
            else if (request.responseCode == 403)
            {
                CheckRefreshController(url, "", 2);
            }

            // 해당 유저의 토큰이 아닐 때
            else if (request.responseCode == 400)
            {
                Debug.Log(request.downloadHandler.text);
            }

            request.Dispose();
        }
    }
}