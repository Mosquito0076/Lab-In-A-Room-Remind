# README.md

## 0. 시작하기 앞서
- [전체 작업 내용](https://github.com/Mosquito0076/Lab-In-A-Room)

<br>

 해당 프로젝트는 학교를 벗어난 '학교 밖 청소년'들에게 과학 실험이 가능한 VR 실험실을 제공해주는 프로젝트였습니다. 팀원간의 역할 분배는 아래와 같습니다.

<br>

권성호 - Unity, 구리 연소 반응 실험, 몸속 맵 구현

황희원 - Unity, 구리 연소 반응 실험, 자기장 관찰 실험, 크로마토 그래피 실험 구현

강보경 - Unity, 불꽃 반응 실험, 우주 맵 구현

이정재 - Unity, 불꽃 반응 실험, 열 팽창과 바이메탈 실험, 정전기 유도 실험 구현

김주원 - Unity & BE, CI/CD, 회원 관리, 메인 페이지 제작 및 전체 UI 연결

홍성목 - Unity & BE, CI/CD, 회원 관리, 실험 보고서 CRUD 구현

<br>

이 프로젝트에서 Unity를 처음으로 사용해보았습니다. 그 과정에서 겪었던 문제, 작성했던 코드 등을 여기에 기록합니다.

<br>

<br>

## 1. 사진 파일 송수신

방구석 실험실에는 사용자가 했던 실험에 대해 확인할 수 있도록 보고서 기능이 포함되어 있습니다. 이 보고서에는 실험 중 찍은 사진을 송신하는 기능이 포함되어 있습니다. 따라서 Unity - Spring Boot 간의 파일 송수신 기능이 필요했습니다.

하지만 Unity를 처음 배웠을 뿐더러, 파일 송수신 기능을 구현해본 적이 없었기 때문에, 아래와 같은 단계를 거치게 되었습니다.

1. 간단한 React 페이지를 만들어, Spring Boot 서버와 로컬 환경에서의 파일 송수신을 구현한다.
2. Unity에서 파일 송신 기능을 만들어, Spring Boot 서버와 로컬 환경에서의 파일 송수신을 구현한다.
3. Spring Boot 서버가 AWS EC2 내부에 파일을 저장하고 불러오는 기능을 구현한다.

위 과정을 통해 작성한 코드를 하나씩 소개해보고자 합니다.

<br>

### 1) React

```js
// web/src/test/Test.js

	  // 내부 저장소 위치의 이미지를 보여줌
	  <img src={imgUrl} style={{ width: "300px" }} alt="img" />
          
          // 파일에 대한 input을 받음
          <input type="file" accept="image/*" onChange={printFile}></input>

		  // 송신 버튼을 누르면 서버로 송신
          <button onClick={saveImg}>송신</button>
```

input tag의 type 값을 file로 줌으로서, 파일을 업로드 가능하게 한 뒤, 파일이 들어오면 printFile 이라는 함수를 실행시킵니다.

송신 버튼을 누르면 saveImg 함수를 통해 서버로 사진 파일을 전송합니다.

<br>

```js
// web/src/test/Test.js

  const [img, setImg] = useState("");
  const [imgUrl, setImgUrl] = useState("");

  const printFile = (e) => {
    
    // img에 저장
    setImg(e.target.files[0]);
    
    // FileReader를 통해 내부 경로 파악
    const reader = new FileReader();
    
    // FileReader가 불러와질 때 이미지 갱신
    reader.onload = () => {
      setImgUrl(reader.result);
    };
    
    // 파일 경로 확인
    reader.readAsDataURL(e.target.files[0]);
  };

```

사진이 업로드 되었을 경우, 아래와 같은 두 가지 동작이 이루어집니다.

1. 해당 파일 데이터를 img 변수에 저장
2. 파일의 내부 경로를 확인하여 img 태그에 연결, 화면에 띄우기

<br>

```js
// web/src/test/Test.js

  const saveImg = (event) => {
    event.preventDefault();

    axios
      .post("/api/report/save", {
        userIdx,
        expIdx: 1,
        repContent: "test",
      })
      .then((res) => {
        console.log(res);
        
        // 사진이 있으면
        if (!!img) {
          
          // FormData 형식으로 담아서 보냄
          const formData = new FormData();
            
          formData.append("image", img);
            
          axios({
            method: "POST",
            url: `/api/report/picture/${userIdx}/${res.data.repIdx}`,
            
            // 서버에서 확인할 수 있도록 multipart/form-data임을 headers에 명시  
            headers: {
              "Content-Type": "multipart/form-data",
            },
            data: formData,
          }).then((res) => {
            console.log(res);
          });
        }
        axios.get(`/api/report/all/${userIdx}`).then((res) => console.log(res));
      })
      .catch((err) => console.error(err));
  };
```

송신을 눌렀을 경우, img 변수에 저장한 사진 파일을 formData 형식으로 담은 뒤, 이를 서버에 전송합니다. 서버에서 파일을 읽을 수 있도록 headers에 Content-Type이 multipart/form-data임을 명시해줍니다.

<br>

### 2) Spring Boot

```java
// back/dream/src/main/java/com/ssafy/dream/controller/ReportController.java

	@PostMapping("/picture/{userIdx}/{repIdx}")
    public ResponseEntity<?> savePicture(@PathVariable("userIdx") Long userIdx, @PathVariable("repIdx") Long repIdx, @RequestParam MultipartFile image) {
        return reportService.savePicture(userIdx, repIdx, image);
    }
```

송신된 사진은 Url mapping에 따라 ReportController의 savePicture로 받게 됩니다.

<br>

```yaml
# back/dream/bin/main/applicaion.yml

spring:
  datasource:
    driver-class-name: com.mysql.cj.jdbc.Driver
    url: jdbc:mysql://localhost:3306/dream?characterEncoding=UTF-8&serverTimezone=UTC
#    url: jdbc:mysql://k7d101.p.ssafy.io:3306/dream?characterEncoding=UTF-8&serverTimezone=UTC
    username: ssafy
    password: ssafy
  servlet:
    multipart:
      maxFileSize: 10MB
      maxRequestSize: 20MB
      
      # 저장할 경로. AWS에서는 Docker Volume을 설정한 경로
      location: /Users/multicampus/Desktop/upload
  #      location: /home/ubuntu/backend/upload
```

설정 파일에 위와 같이 Docker Volume을 통해 저장할 경로가 설정되어 있습니다. 이를 파일 저장 시 사용합니다.

<br>

```java
// back/dream/src/main/java/com/ssafy/dream/service/ReportService.java

    @Value("${spring.servlet.multipart.location}")
    private String bpath;

    @Transactional
    public ResponseEntity<?> savePicture(Long userIdx, Long repIdx, MultipartFile image) {
        Users user = userRepository.findByUserIdx(userIdx);
        Reports report = reportRepository.findByRepIdx(repIdx);
        if(user == null) {
            return new ResponseEntity<>("존재하지 않는 유저입니다", HttpStatus.BAD_REQUEST);
        } else if (report == null) {
            return new ResponseEntity<>("존재하지 않는 보고서입니다", HttpStatus.BAD_REQUEST);
        } else {
            
            // Docker Volume으로 지정된 곳
            String localPath = "C:/Users/multicampus/Desktop/upload";
//            String localPath = "/home/ubuntu/backend/upload";
            
            // 저장할 파일 이름
            String picName = userIdx.toString() + "_" + repIdx.toString() + "_" + image.getOriginalFilename();
            
            // 파일로 변환 및 저장
            File picture = new File(localPath, picName);
            try {
                image.transferTo(picture);
            } catch (IOException e) {
                System.out.println("저장 실패");
            }
            
            
            Path path = Paths.get(bpath+"/"+picName);

            // 쓰기 읽기 허용
            picture.setWritable(true);
            picture.setReadable(true);

            Pictures pictureEntity = Pictures.builder()
                    .picName(picName)
                    .picSize(image.getSize())
                    .picUrl(path.toString())
                    .build();
            pictureRepository.save(pictureEntity);
            report.setPicture(pictureEntity);


            return new ResponseEntity<>(true, HttpStatus.OK);
        }
    }
```

ReportService에서 파일을 저장하고, 읽기 쓰기를 허용해 준 다음, 이름과 경로를 MySQL에 저장합니다.

<br>

### 3) Unity

```c#
// unity/서버 없는 버전/Script/ReportController.cs


    IEnumerator SavePicCo(string url)
    {
		
        // 사진을 MultipartForm 형태로 변경
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

        for (int i = 0; i < images.Count; i++)
        {
            // 파일 이름
            string filename = Path.GetFileName(images[i]);
            
            // byte로 읽어들이기
            byte[] bytes = File.ReadAllBytes(images[i]);
            
            // Multipart 형태로 파일을 바꾸어 리스트에 저장
            formData.Add(new MultipartFormFileSection("images", bytes, filename, "application/octet-stream"));
        }
        
        // 송신 시 서버에서 경계를 확인할 수 있도록 경계 생성
        byte[] boundary = UnityWebRequest.GenerateBoundary();
        
        // 경계값과 함께 묶어줌
        byte[] formSections = UnityWebRequest.SerializeFormSections(formData, boundary);


        using (UnityWebRequest request = UnityWebRequest.Post(url, formData, formSections))
        {
            // header의 Content-type을 multipart/form-data로 변경
            // 경계값을 같이 보내서 읽을 수 있도록 변경
            request.SetRequestHeader("Content-Type", "multipart/form-data; boundary=" + Encoding.UTF8.GetString(boundary, 0, boundary.Length));
            
            // jwt 토큰 추가
            request.SetRequestHeader("Authorization", token);

            yield return request.SendWebRequest();

```

React와 같이, Unity에서도 위와 같이 사진 파일을 보내도록 작성되었습니다. Spring Boot에서 인식이 가능하도록 multipart 형태로 파일을 변형하며, React와는 다르게 경계값을 수동 설정 해줄 필요가 있으므로, 설정을 해준 뒤 UnityWebRequest를 통해 이를 송신합니다.

이를 통해 로컬에서는 문제 없이 AWS EC2의 Spring Boot로 파일 송신이 가능하였지만, Unity에서 Oculus Application을 추출해서 사용하였을 때에는 문제가 있었습니다. Oculus 내부의 스크린샷 사진을 불러올 수가 없었던 것입니다. 문제를 해결하기 위해서는 권한 허가를 얻어야 한다는 글을 읽고, 추가로 권한을 얻는 코드를 작성하였습니다.

<br>

```c#
// unity/서버 없는 버전/Script/PermissionsRationaleDialog.cs


public class PermissionsRationaleDialog : MonoBehaviour
{
    // 창의 크기
    const int kDialogWidth = 300;
    const int kDialogHeight = 100;
    private bool windowOpen = true;

    // 권한 허가 요청
    void DoMyWindow(int windowID)
    {
        // 창을 띄우기
        GUI.Label(new Rect(10, 20, kDialogWidth - 20, kDialogHeight - 50), "���� Ȯ�� ��û");
        GUI.Button(new Rect(10, kDialogHeight - 30, 100, 20), "No");
        if (GUI.Button(new Rect(kDialogWidth - 110, kDialogHeight - 30, 100, 20), "Yes"))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            windowOpen = false;
        }
    }

    void OnGUI()
    {
        // 입력 값에 맞게 창 띄우기
        if (windowOpen)
        {
            Rect rect = new Rect((Screen.width / 2) - (kDialogWidth / 2), (Screen.height / 2) - (kDialogHeight / 2), kDialogWidth, kDialogHeight);
            GUI.ModalWindow(0, rect, DoMyWindow, "Permissions Request Dialog");
        }
    }
```

하지만 위 코드로도 권한 허가를 받을 수는 없었습니다. 최신 버전 Unity에서 Oculus Apk를 추출할 경우, 권한을 얻을 수 없는 이슈가 존재하였기 때문입니다. 스크린 샷 폴더 이외에 게임 자체에서 접근이 허용된 폴더가 있었기에, 해당 폴더에 스크린 샷을 저장하는 방법을 생각해보았지만, 그럴 경우 쓰기 권한이 필요하기 때문에 마찬가지로 할 수 없었습니다.

기능을 수정하기 위해서 전체 프로젝트의 Unity 버전을 낮출 수도 있지만, 그럴 경우 다른 파트에서 또다른 문제가 발생할 수도 있습니다. 따라서 아쉽게도, 직접 찍은 사진을 저장하지 않고 미리 찍어둔 사진 중 원하는 사진을 선택하여 저장하는 방향으로 구현하였습니다.

<br>

<br>

## 2. 트러블 슈팅

### 1) 드래그 앤 드랍이 적용되지 않는 문제

**마우스**를 기준으로 드래그 앤 드랍 기준을 만들었을 때, 오큘러스의 **XR**은 클릭 이벤트의 기준이 다르고, XR 클릭 시의 **좌표계**도 이상해져서 제대로 적용되지 않는 문제가 있었습니다. 이는 컴퓨터 기준 **마우스 커서**의 위치와 오큘러스 기준 **XR 포인터**의 위치가 서로 다른 원리로 동작하고 있었기 때문이었습니다. XR 포인터는 포인터가 점이 아니라 선으로 이루어져, 클릭 시에는 해당 선과 **같은 선상**에 놓여있는 물체들을 Vector 계산을 통해 구하고, 그 중 가장 가까이 있는 물체에 대해 클릭 이벤트가 발생하는 구조였습니다. 따라서 이를 해결하기 위해서는 **사용자의 위치**와 **패널의 위치**, 그리고 그 둘 사이의 포인터의 **각도**를 파악한 뒤, 포인터가 가리키는 위치를 직접 **계산**했어야 했습니다.

<br>

또한 **클릭 이벤트**가 마우스와는 다르게 설정되어 있어, 마우스는 클릭 시 **Pointer Down** 이벤트가 발생하고, 클릭을 해제했을 때 **Pointer Up** 이벤트가 발생하지만, XR은 한 번이라도 클릭했으면 실제로 클릭을 해제하지 않아도 자동으로 해제 이벤트까지 작용하였습니다. 이는 두 가지 문제점 때문에 발생하는 문제였습니다. 하나는 마우스와는 **다른 로직**으로 이루어져 있기 때문이었고, 다른 하나는 **오큘러스 컨트롤러** 자체가 클릭에 대한 기준이 느슨하다는 점이었습니다. 이러한 문제점을 기준으로 생각해보았을 때, 마우스와는 소프트웨어, 하드웨어 적으로 많은 차이가 있는 상황이었습니다. 따라서 오히려 **드래그 앤 드랍**으로 구현할 경우 사용자의 불편함이 증대될 것이라고 판단하였고, 이를 폐기하여 원시적인 **포인트 앤 클릭** 형식으로 기능을 구현하게 되었습니다.

<br>

![1-1](https://file.notion.so/f/s/2176b0dc-f717-4dcb-9c81-fe7938552544/%EC%83%88%EB%A1%9C%EC%9A%B4_%ED%94%84%EB%A1%9C%EC%A0%9D%ED%8A%B8.gif?id=2a9afdf9-3748-49f2-8e4c-876da0d26bc5&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681372318708&signature=MAJLNAdCLREpcvgHHNqfkLXFmSmbybIR2h_MLcrVhBU&downloadName=%EC%83%88%EB%A1%9C%EC%9A%B4+%ED%94%84%EB%A1%9C%EC%A0%9D%ED%8A%B8.gif)

<br>

### 2) 동일한 스크립트를 사용 시 변수까지 함께 공유하는 문제

보고서 작성 기능을 제작하기 위해서는 10개의 **선택지**와 5개의 **빈칸**이 있어야만 했습니다. 이 선택지와 빈칸들은 서로 다른 위치와 텍스트를 갖지만 수행하는 기능은 완벽하게 같았기 때문에, 처음에는 **같은 스크립트**를 서로 다른 선택지와 빈칸에 컴포넌트로 추가해주었습니다. 그러나 이 방법에는 치명적인 단점이 있었는데, 바로 스크립트가 개별적으로 ‘생성되어서’ 컴포넌트로 추가되는 것이 아니라, 스크립트 자체가 하나의 컴포넌트이고 이를 추가한다는 것은 그저 **연결**하는 것 뿐이어서 스크립트 **내부 변수**들이 전부 공유되고 있었던 것입니다. 따라서 **이벤트**가 발생하였을 때 그 위치를 특정할 수 없어, 모든 이벤트가 엉망으로 작동되는 것을 확인할 수 있었습니다.

<br>

![2-1](https://file.notion.so/f/s/de775502-b540-4029-9ead-bda0ae1880e0/Untitled.png?id=4df4e313-0d0e-44b6-933c-3f5de51c6a39&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375291726&signature=tIzIpQQBGaLmmY_wD3-7gcPpl54SMloORrVJLjlSiGk&downloadName=Untitled.png)

> 당시 작성했던, 수정 이전 버전 코드

일일이 스크립트를 만들어주는 것 또한 한 가지 방법이었으나, 그러면 코드의 **재사용성** 측면이 크게 무너지는 데에 더해, 보고서 1개 당 빈칸 5개 이므로 보고서 10개 기준으로 50 + 10 개, **총 60개**의 스크립트를 작성했어야 했습니다. 그래서 다른 방법을 찾아보기 시작했습니다.

해결 방법을 찾던 중, 스크립트에서는 **name** 메서드를 통해 연결된 게임 오브젝트의 이름을 받아올 수 있다는 것을 알아냈습니다. 이 점에 착안하여 게임 오브젝트의 이름으로 이벤트의 **발생 위치를 특정**시키도록 코드를 작성하였습니다.

<br>

![2-2](https://file.notion.so/f/s/f37dfabd-f881-4030-b90b-eec1e54a07c7/Untitled.png?id=2d30989b-752a-4411-9acc-d8cba8461db0&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375323102&signature=u1c7KZ1dRqub2h1sEWXjjbeJf6YXyYtFDf-o9ihZcLw&downloadName=Untitled.png)

![2-3](https://file.notion.so/f/s/8e44e29d-5f4c-42e5-bcc5-826a02c599f1/Untitled.png?id=d6119e20-e275-4a73-a953-7f0fd92fa126&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375337044&signature=AxtBaM7naCWRCSTLu2CLNAUdYTjcFD_bQjfF_VmXKfo&downloadName=Untitled.png)

> 최종적으로 사용했던 게임 오브젝트 구조와 코드

이러한 로직으로 인해 **로딩 속도**가 유의미할 정도로 느려지게 된다면 다른 해결책을 찾아볼 생각이었으나, 다행히 육안으로 확인할 수 있는 차이가 발생하지 않아서 많은 코드를 위와 같은 형태로 사용하게 되었습니다.

<br>

### 3) InActive Object의 컴포넌트에 접근할 수 없는 문제

사진 및 보고서 내용 전송을 위해서는, **보고서 Object**의 컴포넌트 중 하나인 **ReportSettingScript**에 저장된 정보에 접근할 필요가 있었습니다.

하지만 보고서 제출은 실험 종료 시에 이루어지고, 이 때 보고서는 **InActive** 되어 있는 상태였습니다. 그런데 InActive된 Object의 **스크립트 컴포넌트**에는 접근할 수 없다는 문제가 있었습니다. 따라서 이를 해결하기 위해, 보고서 제출 시 잠시 Object를 **Active** 한 뒤, 서버와의 통신 후 다시 InActive 하는 방법을 채택하였습니다. 만약 보고서가 너무 오래 보이고 눈에 띈다면 **투명도** 조절로 안 보이게 할 계획이었지만, 인식하기 힘들 정도로 빠르게 사라지는 것을 확인하여서 투명도 적용까지는 하지 않고 끝내게 되었습니다.

<br>

![3-1](https://file.notion.so/f/s/3f945992-cfce-42c2-8f14-6456905aadd9/Untitled.png?id=fea94cdc-af46-4213-890b-5fac7ccb9e91&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375392709&signature=K7pGgdNr09kPY7IsEoYSBpbcaJruSHLQFTLlW3BHHBE&downloadName=Untitled.png)

> 보고서 전송 시, 보고서 Object를 활성화 시키고 저장하는 것을 확인할 수 있다.

<br>

![3-2](https://file.notion.so/f/s/aed0ee2f-1e34-44f3-8c32-862c41249c1a/Untitled.png?id=fd928156-6fdc-4a27-b41c-00b9178409b7&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375428267&signature=iSeHumX-iowgTIyRI0x8w0nlVt6wjSXmdVfs8tbseGs&downloadName=Untitled.png)

> 보고서 저장 성공 시, 다시 Object를 비활성화 한 다음, 퀴즈 패널을 활성화 시키는 모습을 확인할 수 있다.

<br>

### 4) 서버와 날짜 정보가 일치하지 않는 문제

보고서 작성 시 저장 항목 중에는 **날짜**가 포함되어 있습니다. 그런데 C#과 UnityWebRequest에 대해 학습하던 중, C#과 Java의 **DateTime**의 양식은 다르며, 이를 사용하기 위해서는 **변환**이 필요하다는 것을 알게 되었습니다. 하지만 Front - Unity와 Back - Spring Boot의 조합은 드물어서 이에 대한 정보를 찾아보기 힘들었고, 따라서 보고서 저장 시 날짜는 **서버 사이드**에서 자체 DateTime으로 저장하도록 하였습니다. 또한 보고서 조회 시 날짜는 바로 Unity에서 쓸 수 있도록 **문자열**로 변환하여 전송하였습니다.

![4-1](https://file.notion.so/f/s/1af66235-1c92-4e46-b660-c094c1329abf/Untitled.png?id=daed31b6-5aef-4beb-b49f-2328d626dcc2&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375479905&signature=-f_L5OaZS8G84_aE5RYzrBgQEUAvbQdNIDVePfkp6Ls&downloadName=Untitled.png)

> String으로 변환하여 날짜를 전송

하지만 이 방법에는 오류가 있었는데, 바로 서버가 한국에 있지 않다는 것이었습니다. 그래서 **시차**가 발생해, 서버에 저장되는 시각은 한국에 비해 **9시간** 빠른 시각이었습니다. 이를 해결하는 방법은

1. 서버의 시간 자체를 변경한다
2. 저장 시 9시간을 더하여 저장한다

와 같이 2가지 방법이 있었습니다.

<br>

어느 것을 사용하여도 기능 상 문제는 없었기 때문에, 빠르게 해결할 수 있는 2번으로 해결하였습니다. 1번의 해결방법은 [링크](https://umanking.github.io/2021/08/10/spring-boot-server-timezone/)와 같이 할 수 있습니다.

![4-2](https://file.notion.so/f/s/fc7413db-50b9-4d2c-945a-ec1c391b168d/Untitled.png?id=e6bded8b-297a-4e29-91ac-dd2698f80f1e&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375505060&signature=OoELRRjuXS4mAID8gWlcBxT5ZlMwWMnkeeF9pxcFPkE&downloadName=Untitled.png)

> Calender를 이용하여 시간을 더하고 저장함

<br>

### 5) PlayerPrefs에 배열을 저장할 수 없는 문제

자율 프로젝트가 마무리되면서, **서버**가 내려갔기 때문에 서버가 없이도 작동되도록 기능을 수정할 필요가 있었습니다. 서버가 필요한 부분은 로그인, 회원가입과 **보고서 CRUD** 였습니다. 보고서 기능은 제가 담당했었기 때문에 이를 수정하여야 했는데, 문제가 있었습니다. 버전 오류로 인해 **내부 쓰기권한**을 얻지 못하여 이를 Unity의 자체 기능인 **PlayerPrefs**로 해결해야 했던 것입니다.

PlayerPrefs는 **Local**에 데이터를 **저장**하거나 불러올 수 있는, Unity에서 자체적으로 지원하는 기능입니다. 하지만 저장하거나 불러올 수 있는 항목은 **Float**, **Int**, **String** 뿐이었습니다. 하지만 보고서의 내용은 **배열**이기에, 이를 String으로 **변환** 후 읽고 쓸 수 있도록 할 필요가 있었습니다.

또한 단순히 **ToString**으로 변환하면 이를 다시 분해해낼 수 없었습니다. 따라서 **Json 변환**을 통해 문자열을 생성하고, 이를 다시 **Parsing**하는 방법을 채택하였습니다.

![5-1](https://file.notion.so/f/s/1552df5c-2711-42ff-bf4a-762ba794f64e/Untitled.png?id=346cc06c-2ddf-485d-a27d-97154812511f&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375550406&signature=OSXouzQC87XdhnIy14m2Z07uJg4EMW0kT4UiIh2TiiI&downloadName=Untitled.png)

> 저장을 위해 배열을 객체에 넣어준다.

<br>

![5-2](https://file.notion.so/f/s/1cf27bf1-3b3e-4cbe-9ccb-9e58a9922977/Untitled.png?id=1e656fd5-a7ba-45ca-aa91-5e3c85bae4f4&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&expirationTimestamp=1681375684449&signature=_aFyNP8un6bPfLiJqgU4RPpzAU-WAOotkgchZRbtToc&downloadName=Untitled.png)

> JsonUtility를 이용하여 이를 객체로 Parsing 해준다. 최대 1MB까지 저장할 수 있지만, 만에 하나를 대비하여 보고서는 120개 까지만 저장할 수 있도록 한다.

<br>

<br>

## 3. UCC

이번 프로젝트에서 다시 UCC 제작을 담당하게 되었습니다. UCC 기획 당시 시연 영상을 필수로 넣으려고 했지만, 시연이 프로젝트 종료 직전이어서 편집에 쓸 수 있는 시간은 단 하루 뿐이었습니다.

11/20 (일) : BGM 선정, 기본 틀 설정, 영상 편집 및 최종 수정

<br>

하루 뿐이었기 때문에, 편집에 크게 시간을 할애할 수가 없었습니다. 따라서 최대한 있는 자료들을 이용하여 전달할 사항을 전부 전할 수 있도록 구성하였습니다. 또한 BGM 선정과 자막을 넣는 부분에서는 황희원 팀원의 도움을 받았습니다. 풀 버전 UCC는 크게 4가지 파트로 이루어져 있습니다.

1. 기획 의도 : 방구석 실험실의 기획 의도에 대해 설명하며, 중간 발표 PPT로 구성
2. 기능 소개 : 방구석 실험실의 기능에 대해 설명하며, 실제 사용 영상으로 구성
3. 시연 및 체험 : 인천 꿈드림 센터에서의 시연 및 체험 영상으로 구성
4. 인터뷰 : 인천 꿈드림 센터의 선생님과 청소년들의 인터뷰 영상으로 구성

나레이션은 TTS로 오해 받을 정도로 목소리가 매우 좋은 강보경 팀원에게 부탁하여 진행하게 되었습니다.

<br>

또한 PPT에 추가한 영상은, 1번과 2번을 제외하고 3번과 4번을 압축하여 약 2분 정도의 영상으로 제작하였습니다.

<br>

![썸네일](https://user-images.githubusercontent.com/95673624/231238314-a03a3877-0fd0-4a75-b65a-9bb69656269c.png)

[방구석 실험실 UCC (Full Ver) - 8분](https://www.youtube.com/watch?v=oWvl9y9Yvgk)

[방구석 실험실 UCC (PPT Ver) - 2분](https://www.youtube.com/watch?v=m6aFv76pGqI)

<br>

기획 의도면에서 좋은 평가를 받아, UCC가 사회 공헌 부문에서 특별상을 수상할 수 있었습니다.

![수상](https://metal-carver-67b.notion.site/image/https%3A%2F%2Fs3-us-west-2.amazonaws.com%2Fsecure.notion-static.com%2F2a6a888e-4deb-4e83-974e-c014220d47f5%2FUntitled.png?id=f44da11d-3faf-46fd-856e-5bca0cb1e9d9&table=block&spaceId=cd623665-cea2-48e3-bea6-f67f995aedb2&width=2000&userId=&cache=v2)

<br>

<br>

## 4. 마무리하며

마지막 프로젝트에서는 다양한 직무를 맡았습니다. 부팀장, UCC, FE, BE 등 전반적인 프로젝트에 전부 참여할 수 있었습니다. 이전까지는 발표 또는 프로젝트 관리 보다 결과물의 완성도를 올리는 것에 우선하여 발표와 팀장을 맡지 않고 있었습니다. 하지만 이번 기회에 둘 다 맡아보면서, 여러 역할을 맡으면서도 충분히 결과물의 완성도를 올릴 수 있다는 것을 알게 되었고, 발표와 매니지먼트 또한 개발만큼 재밌음을 알게 되었습니다. 

<br>

또한 실제 사용자 대상 시연은 처음이었는데, 제가 만든 프로그램을 사용하는 모습을 보면서 개발자로서의 보람을 느낄 수 있었습니다. 정말 좋은 경험이었다고 생각합니다. 이러한 기회를 만들어준 권성호 팀장님과 인천 미추홀구 꿈드림 센터분들께 감사하다는 말을 전하고 싶습니다.

<br>

추후 제가 어떠한 일을 하게 되더라도, 방구석 실험실만은 정말 잊지 못할 것 같습니다. 앞으로도 이 열정을 잊지 않고 살아갈 수 있도록 노력해야겠다고 생각하게 되었습니다.

<br>

<br>

## 5. 참고자료

\<React 관련\>

[리액트로 이미지 업로드& 미리보기](https://velog.io/@gay0ung/%EB%A6%AC%EC%95%A1%ED%8A%B8%EB%A1%9C-%EC%9D%B4%EB%AF%B8%EC%A7%80-%EC%97%85%EB%A1%9C%EB%93%9C-%EB%AF%B8%EB%A6%AC%EB%B3%B4%EA%B8%B0)

[[React] 이미지 파일 업로드 기능 구현](https://velog.io/@yeogenius/React-%ED%81%B4%EB%9D%BC%EC%9A%B0%EB%93%9C-%EC%8A%A4%ED%86%A0%EB%A6%AC%EC%A7%80%EC%97%90-%EC%9D%B4%EB%AF%B8%EC%A7%80-%ED%8C%8C%EC%9D%BC-%EC%97%85%EB%A1%9C%EB%93%9C-%ED%95%98%EA%B8%B0)

<br>

\<Spring Boot 관련\>

[스프링부트 이미지 저장, 전송](https://velog.io/@yyong3519/Springboot-%EC%9D%B4%EB%AF%B8%EC%A7%80-%EC%B2%98%EB%A6%AC)

[[Spring Boot] 게시판 구현 4 - 다중 파일(이미지) 업로드 MultipartFile](https://velog.io/@yu-jin-song/SpringBoot-%EA%B2%8C%EC%8B%9C%ED%8C%90-%EA%B5%AC%ED%98%84-4-MultipartFile-%EB%8B%A4%EC%A4%91-%ED%8C%8C%EC%9D%BC%EC%9D%B4%EB%AF%B8%EC%A7%80-%EC%97%85%EB%A1%9C%EB%93%9C)

[[Spring Boot] 게시판 구현 하기 (4) - 파일 업로드 & 다운로드](https://kyuhyuk.kr/article/spring-boot/2020/07/22/Spring-Boot-JPA-MySQL-Board-Post-File-Upload-Download)

[스프링부트 이미지 저장, 전송](https://velog.io/@yyong3519/Springboot-%EC%9D%B4%EB%AF%B8%EC%A7%80-%EC%B2%98%EB%A6%AC)

[[Spring Boot] 이미지 파일 경로 외부에 설정하기 with yml](https://blog.jiniworld.me/28)

[[Spring Boot] 이미지 스토리지 서버 만들기](https://wonmocyberschool.tistory.com/66)

[[Spring] 로컬 저장소의 이미지 파일 웹에서 보여주기](https://dev-gorany.tistory.com/17)

<br>

\<Docker 관련\>

[배포 자동화 (2) 백엔드, 프론트엔드 도커 이미지 빌드](https://sinawi.tistory.com/371)

[Docker container 접속하기](https://believecom.tistory.com/757)

[[Docker 기본(5/8)] Volume을 활용한 Data 관리](https://medium.com/dtevangelist/docker-%EA%B8%B0%EB%B3%B8-5-8-volume%EC%9D%84-%ED%99%9C%EC%9A%A9%ED%95%9C-data-%EA%B4%80%EB%A6%AC-9a9ac1db978c)

[[Docker] nginx 띄우고 spring boot (jar 파일) 연동하기 (1) - 컨테이너 안에 폴더와 로컬 폴더 동기화](https://thalals.tistory.com/343)

[Jenkins에 GitLab 저장소 연동](https://velog.io/@hmyanghm/Jenkins%EC%97%90-GitLab-%EC%A0%80%EC%9E%A5%EC%86%8C-%EC%97%B0%EB%8F%99)

[[CI/CD] Gitlab과 Jenkins로 CI/CD 구축하기](https://velog.io/@hanif/Gitlab%EA%B3%BC-Jenkins%EB%A1%9C-CICD-%EA%B5%AC%EC%B6%95%ED%95%98%EA%B8%B0)

<br>

\<Nginx 관련\>

[[CI/CD] Gitlab + Jenkins + Nginx + Docker + AWS EC2 - 무중단 배포](https://gksdudrb922.tistory.com/236)

[[Ubuntu] 우분투 방화벽(UFW) 설정](https://webdir.tistory.com/206)

[[AWS] EC2 인스턴스에 Nginx 적용하기](https://velog.io/@jkijki12/%EB%B0%B0%ED%8F%AC-Aws-%EC%9D%B8%EC%8A%A4%ED%84%B4%EC%8A%A4%EC%97%90-Nginx-%EC%A0%81%EC%9A%A9%ED%95%98%EA%B8%B0)

[기존 프로젝트를 무중단 배포로 바꿔보자! (via jenkins)](https://hyunminh.github.io/nonstop-deploy/#nginx-%EC%84%A4%EC%A0%95%ED%95%98%EA%B8%B0)

[[AWS] Spring, Nginx, Docker로 무중단 배포하기 - 2탄](https://devlog-wjdrbs96.tistory.com/317)

[NginX SSL 인증서 설치/적용 가이드](https://www.sslcert.co.kr/guides/NGINX-SSL-Certificate-Install)

[[1. NGINX 정적(static)파일 연결하기](https://amuse.tistory.com/10)](https://amuse.tistory.com/10)

[nginx HTTP 로 들어오면 강제로 HTTPS 로 전환하도록 설정하기(force redirect to SSL)](https://www.lesstif.com/system-admin/nginx-http-https-force-redirect-to-ssl-113344694.html)

<br>

\<Unity 관련\>

[Unity 공식 문서](https://docs.unity3d.com/2017.3/Documentation/Manual/UnityWebRequest-SendingForm.html)

[C# 공식 문서](https://learn.microsoft.com/ko-kr/dotnet/api/system.io.directory.getfiles?view=net-6.0)

[오큘러스 퀘스트 2 첫 VR 앱 제작 및 빌드 문제 해결](https://projecteli.tistory.com/194)

[유니티) 오큘러스 퀘스트2 빌드,설치하기](https://workdiarysometimesnot.tistory.com/7)

[[Unity + Oculus] VR 개발하기 - 1 (환경 구현, 개발자 등록)](https://yoonstone-games.tistory.com/m/102)

[[C#] 웹 서버로 HttpWebRequest를 이용하여 파일 업로드하는 방법](https://nowonbun.tistory.com/19)

[[C#] 유니티에서 FTP로 파일 전송 및 파일 다운로드](https://mgtul.tistory.com/20?category=497960)

[How can I create Image from image file on the hard disk and display the image in image ui ?](https://forum.unity.com/threads/how-can-i-create-image-from-image-file-on-the-hard-disk-and-display-the-image-in-image-ui.1044334/)

[Unitywebrequest using multipart/form-data](https://forum.unity.com/threads/unitywebrequest-using-multipart-form-data.1280840/)

[[C#]현재 날짜 및 시간 가져오기](https://developer-talk.tistory.com/147)

[[Unity] 게임 개발 - 드래그 앤 드롭](https://krapoi.tistory.com/44)

[유니티 자식 오브젝트 찾는 법](https://angliss.cc/unity-getchild-gameobject/)

[유니티(unity)에서 import한 이미지 파일 해상도가 안좋아 보이는 경우 (texture type 설정)](https://clack.tistory.com/35)

[[Unity]EventSystem을 이용해 아이템UI 드래그 및 다른 슬롯에 등록하기(IDragHandler, IDropHandler)](https://penguinofdev.tistory.com/33)

[유니티에서 오큘러스 VR 세팅하기](https://coding-of-today.tistory.com/2)

[I'm having a problem dragging and dropping an object in VR using Unity](https://stackoverflow.com/questions/67623272/im-having-a-problem-dragging-and-dropping-an-object-in-vr-using-unity)

[유니티 WWWform 이용한 multipart 파일전송 헤더 오류.](https://bulkdisk.tistory.com/89)
