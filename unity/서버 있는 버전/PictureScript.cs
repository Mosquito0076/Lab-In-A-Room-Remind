using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;


[System.Serializable]
public class ReportPicture
{
    public int picIdx;
    public string picName;
    public string picUrl;
}

[System.Serializable]
public class AllReportPicture
{
    public List<ReportPicture> pictures;
}

public class CheckPictureToken
{
    public string userId;
    public string refresh;
}

public class PictureScript : MonoBehaviour
{
    // 실험 시작 이후 사진만
    private DateTime date;

    // 필터링을 위해 문자열 화
    private string startedDateTime;

    // 몇 번 페이지인지 기록하기
    private int pageIdx = 0;

    // 사진 입힐 객체
    private GameObject picture;

    // 프리뷰
    public GameObject preview;

    // 선택된 사진들
    public List<int> selected = new List<int>();

    // 버튼들
    public GameObject upButton;
    public GameObject downButton;

    // jwt 토큰
    private string token;

    // 실험 사진들
    private List<ReportPicture> pictures = new List<ReportPicture> { };

    // 실험 인덱스
    private int expIdx;



    void Awake()
    {
        // 현재 시간 불러오기 및 필터링 준비
        date = DateTime.Now;
        startedDateTime = $"com.oculus.shellenv-{date.ToString($"yyyyMMdd-HHmmss")}.jpg";

        expIdx = PlayerPrefs.GetInt("expIdx");
        // expIdx = 1;
        token = PlayerPrefs.GetString("access");
        // token = "Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJoc21rMDA3NiIsInJvbGVzIjpbIlJPTEVfVVNFUiJdLCJpYXQiOjE2NjkwMTg1NTIsImV4cCI6MTY2OTEwNDk1Mn0.nCHKbq5QMM5moOlJuTJB39C2bSX-6dOUI9-csLK4KIg";
        LoadPictures();

    }

    private void OnEnable()
    {
        ResetPictures();
    }

    private void LoadPictures()
    {
        string url = $"https://k7d101.p.ssafy.io/api/report/exp/pictures/{expIdx}";
        StartCoroutine(LoadExpPicture(url));
    }

    private void ResetPictures()
    {
        // 재로딩을 위한 리셋 처리
        for (int a = 0; a < 8; a++)
        {
            GameObject picture = transform.Find($"Picture_{a}").gameObject;
            picture.GetComponent<Outline>().effectColor = Color.black;
            picture.transform.Find($"Check_mark_{a}").gameObject.SetActive(false);
            picture.SetActive(false);
        }

        for (int j = 0; j < pictures.Count - pageIdx * 8; j++)
        {
            // 사진은 한 번에 8개만
            if (j == 8)
            {
                break;
            }

            else
            {
                StartCoroutine(LoadExpPicCo(pictures[j + pageIdx * 8].picUrl, j));
                // 선택된 사진이면 배경 변경
                if (selected.Contains(pictures[pageIdx * 8 + j].picIdx))
                {
                    GameObject picture = transform.Find($"Picture_{j}").gameObject;
                    picture.GetComponent<Outline>().effectColor = Color.magenta;
                    picture.transform.Find($"Check_mark_{j}").gameObject.SetActive(true);
                }
            }
        }

        // 사진이 저장된 경로
        // string path = "C:/Users/multicampus/Desktop/Screenshots";
        // string path = "/storage/emulated/11/Oculus/Screenshots";
        // string path = "내 PC/Quest 2/내부 공유 저장용량/Oculus/Screenshots";
        // string path = "/sdcard/my_folder/my_file";


        // 페이지에 따라 인덱스 버튼 수정
        upButton.SetActive(pageIdx != 0);
        downButton.SetActive((pageIdx + 1) * 8 < pictures.Count);

    }

    // 사진 주소 불러오기
    IEnumerator LoadExpPicture(string url)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", token);
            yield return request.SendWebRequest();

            // 잘못된 요청
            if (request.responseCode == 404)
            {
                Debug.Log(request.downloadHandler.text);
            }

            // access 만료시
            else if (request.responseCode == 403)
            {
                CheckPictureToken ck = new CheckPictureToken
                {
                    userId = PlayerPrefs.GetString("userId"),
                    refresh = PlayerPrefs.GetString("refresh")
                };
                string checkJson = JsonConvert.SerializeObject(ck);
                using (UnityWebRequest tokenRequest = UnityWebRequest.Post("https://k7d101.p.ssafy.io/api/user/check", checkJson))
                {
                    byte[] checkJsonToSend = new System.Text.UTF8Encoding().GetBytes(checkJson);
                    tokenRequest.uploadHandler = new UploadHandlerRaw(checkJsonToSend);
                    tokenRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
                    tokenRequest.SetRequestHeader("Content-Type", "application/json");

                    yield return tokenRequest.SendWebRequest();


                    // refresh 만료시
                    if (tokenRequest.responseCode == 403)
                    {
                        PlayerPrefs.DeleteAll();
                        SceneManager.LoadSceneAsync("children_room");
                    }

                    // 해당 유저의 토큰이 아닐 때
                    else if (tokenRequest.responseCode == 400)
                    {
                        Debug.Log(tokenRequest.downloadHandler.text);
                    }

                    // 성공 (Access 재발급)
                    else if (tokenRequest.responseCode == 200)
                    {
                        JObject obj = JObject.Parse(tokenRequest.downloadHandler.text);
                        token = "Bearer " + (string)obj["access"];
                        PlayerPrefs.SetString("access", token);
                        StartCoroutine(LoadExpPicture(url));
                    }
                }
            }

            // 해당 유저의 토큰이 아닐 때
            else if (request.responseCode == 400)
            {
                Debug.Log(request.downloadHandler.text);
            }

            // 성공
            else if (request.responseCode == 200)
            {
                pictures = JsonUtility.FromJson<AllReportPicture>(request.downloadHandler.text).pictures;
                ResetPictures();
            }

            // 오류
            else
            {
                Debug.Log(request.downloadHandler.text);
            }
            request.Dispose();
        }
    }


    // 1. 사진 위에 호버했을 때 큰 화면 띄우기

    public void OpenPreview(int n)
    {
        preview.GetComponent<RawImage>().texture = transform.Find($"Picture_{n}").gameObject.GetComponent<RawImage>().texture;
        preview.SetActive(true);
    }

    public void ClosePreview()
    {
        preview.GetComponent<RawImage>().texture = null;
        preview.SetActive(false);
    }
    
    // 2. 선택 시 저장하여 우상단 체크 표시 및 카운트, 테두리 보라색
    public Boolean SelectPicture(int m)
    {
        // 이미 있으면 삭제 & 취소
        if(selected.Contains(pictures[pageIdx * 8 + m].picIdx))
        {
            selected.Remove(pictures[pageIdx * 8 + m].picIdx);
            transform.Find("Selected_count").GetComponent<Text>().text = $"{selected.Count}";
            if (selected.Count == 4)
            {
                transform.Find("Selected_count").GetComponent<Text>().color = Color.white;
            }
            return false;
        }

        // 없으면 추가 & 활성화
        else
        {
            selected.Add(pictures[pageIdx * 8 + m].picIdx);
            transform.Find("Selected_count").GetComponent<Text>().text = $"{selected.Count}";
            if (selected.Count == 5)
            {
                Color color;
                ColorUtility.TryParseHtmlString("#AD34FFFF", out color);
                transform.Find("Selected_count").GetComponent<Text>().color = color;
            }
            return true;
        }

    }


    IEnumerator LoadExpPicCo(string url, int num)
    {
        using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(url))
        {
            yield return imgReq.SendWebRequest();
            if (imgReq.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(imgReq.error);
            }
            else
            {
                picture = transform.Find($"Picture_{num}").gameObject;
                picture.SetActive(true);
                picture.GetComponent<RawImage>().texture = ((DownloadHandlerTexture)imgReq.downloadHandler).texture;
            }
            imgReq.Dispose();
        }
    }


    // 3. 페이지 넘기는 기능 구현 (위, 아래로 바꾸고, 맨 위와 맨 아래에서는 그 방향으로 못 가게 해야함) + 마지막 페이지에서 사진이 빌 경우 off 시켜야함
    public void Pagination(int k)
    {
        pageIdx += k;
        ResetPictures();
    }

}
