using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.Android;

public class PictureScript : MonoBehaviour
{
    // 실험 시작 이후 사진만
    private DateTime date;

    // 필터링을 위해 문자열 화
    private string startedDateTime;

    // 필터링 된 이미지들의 경로 저장
    private List<string> imgPaths = new List<string>();

    // 몇 번 페이지인지 기록하기
    private int pageIdx = 0;

    // 사진 입힐 객체
    private GameObject picture;

    // 프리뷰
    public GameObject preview;

    // 선택된 사진들
    public List<string> selected = new List<string>();

    // 버튼들
    public GameObject upButton;
    public GameObject downButton;

    GameObject dialog = null;

    void Awake()
    {
        // 현재 시간 불러오기 및 필터링 준비
        date = DateTime.Now;
        startedDateTime = $"com.oculus.shellenv-{date.ToString($"yyyyMMdd-HHmmss")}.jpg";
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead)) 
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            dialog = new GameObject();
        }
        else { ResetPictures(); }
    }

    private void OnEnable()
    {
        ResetPictures();
    }

    private void OnGUI()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {

            dialog.AddComponent<PermissionsRationaleDialog>();
        }
        else if (dialog != null)
        {
            Destroy(dialog);
        }
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



        // 사진이 저장된 경로
        // string path = "C:/Users/multicampus/Desktop/Screenshots";
        string path = "/storage/emulated/11/Oculus/Screenshots";
        // string path = "내 PC/Quest 2/내부 공유 저장용량/Oculus/Screenshots";
        // string path = "/sdcard/my_folder/my_file";



        // 사진 불러오는 로직
        string[] images = Directory.GetFiles(path, "*jpg");
        
        // 최신순으로 불러오기 위해 역순으로
        Array.Reverse(images);

        imgPaths = new List<string>{ };

        // 사진을 순서대로 필터링
        // **********나중에는 대소관계를 바꿔야 한다**********
        for (int i = 0; i < images.Length; i++)
        {
            if (string.Compare(Path.GetFileName(images[i]), startedDateTime) >= 0)
            {
                imgPaths.Add(images[i]);
            }
        }

        // 사진 띄우기
        for (int j = 0; j < imgPaths.Count - pageIdx * 8; j++)
        {
            // 사진은 한 번에 8개만
            if (j == 8)
            {
                break;
            }

            else
            {
                picture = transform.Find($"Picture_{j}").gameObject;
                byte[] bytes = File.ReadAllBytes(imgPaths[pageIdx * 8 + j]);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                picture.GetComponent<RawImage>().texture = texture;
                picture.SetActive (true);

                // 선택된 사진이면 배경 변경
                if (selected.Contains(imgPaths[pageIdx * 8 + j]))
                {
                    picture.GetComponent<Outline>().effectColor = Color.magenta;
                    picture.transform.Find($"Check_mark_{j}").gameObject.SetActive(true);
                }
            }
        }

        // 페이지에 따라 인덱스 버튼 수정
        upButton.SetActive(pageIdx != 0);
        downButton.SetActive((pageIdx + 1) * 8 < imgPaths.Count);

    }
    
    // 1. 사진 위에 호버했을 때 큰 화면 띄우기

    public void OpenPreview(int n)
    {
        byte[] bytes = File.ReadAllBytes(imgPaths[pageIdx*6+n]);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);
        preview.GetComponent<RawImage>().texture = texture;
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
        if(selected.Contains(imgPaths[pageIdx * 8 + m]))
        {
            selected.Remove(imgPaths[pageIdx * 8 + m]);
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
            selected.Add(imgPaths[pageIdx * 8 + m]);
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


    // 3. 페이지 넘기는 기능 구현 (위, 아래로 바꾸고, 맨 위와 맨 아래에서는 그 방향으로 못 가게 해야함) + 마지막 페이지에서 사진이 빌 경우 off 시켜야함
    public void Pagination(int k)
    {
        pageIdx += k;
        ResetPictures();
    }

}
