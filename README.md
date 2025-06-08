# poc-mistral-ocr

โปรเจกต์นี้เป็นตัวอย่างการใช้งาน OCR (Optical Character Recognition) ร่วมกับโมเดล Mistral สำหรับแปลงไฟล์ PDF หรือรูปภาพให้เป็นข้อความ


ศึกษารายละเอียดเชิงลึกได้ที่
- [Youtube](https://youtu.be/OHtEUis2454)
- [Podcast](https://open.spotify.com/episode/1Tx1BFoCIzsuQX15VTUPVE?si=kiLig-bXS7yUjJZK1LY78w)
- [Blog ใน Medium](https://medium.com/t-t-software-solution/mistral-ocr-%E0%B8%94%E0%B9%89%E0%B8%A7%E0%B8%A2-net-%E0%B8%88%E0%B9%89%E0%B8%B2%E0%B8%B2-55a16ed3e6cc)
- [Blog ใน Project นี้](./blog.md)

## คุณสมบัติ

- รองรับไฟล์ PDF และรูปภาพ
- แปลงข้อความจากเอกสารที่สแกนหรือรูปภาพ
- ใช้งานโมเดล Mistral เพื่อประมวลผลข้อความ

## โครงสร้างโปรเจกต์

```
POCMistralOCR/
├── appsettings.json
├── Program.cs
├── docs/
│   ├── input/
│   └── output/
└── ...
```
## ตัวอย่างผลลัพธํจากการทำ OCR
- [Text Only](./POCMistralOCR/docs/output/1.Scanned/1.MistralOCR/page-1/page-1.md)
- [Table Only](./POCMistralOCR/docs/output/2.TablePure/1.MistralOCR/page-1/page-1.md)
- [Image Only](./POCMistralOCR/docs/output/3.Image/1.MistralOCR/page-1/page-1.md)
- [Text + Table + Image](./POCMistralOCR/docs/output/4.TextWithTableWithImage/1.MistralOCR/)

## การใช้งานและขอ Access Token สำหรับ Mistral OCR

- สามารถศึกษาข้อมูลเกี่ยวกับ Mistral OCR ได้ที่เว็บไซต์ทางการ: https://docs.mistral.ai/capabilities/OCR/basic_ocr/
- การขอ Access Token (API Key):
  1. สมัครสมาชิกหรือเข้าสู่ระบบที่ https://console.mistral.ai/
  2. ไปที่เมนู API Keys แล้วสร้าง API Key ใหม่
  3. นำ API Key ที่ได้ไปใส่ในไฟล์ `POCMistralOCR/appsettings.json` ในส่วน `MistralOCR:ApiKey`

## NuGet Packages ที่ใช้ในโปรเจกต์นี้

| Package Name                        | Version   | ใช้เพื่ออะไร                                    |
|-------------------------------------|-----------|-------------------------------------------------|
| Docnet.Core                        | 2.6.0     | สำหรับอ่านและแปลงไฟล์ PDF เป็นภาพ (split PDF)   |
| Microsoft.Extensions.Configuration | 9.0.5     | สำหรับโหลดและจัดการคอนฟิก (เช่น appsettings)   |
| Microsoft.Extensions.Configuration.Json | 9.0.5 | สำหรับอ่านคอนฟิกจากไฟล์ JSON (appsettings.json) |
| SkiaSharp                           | 3.119.0   | สำหรับประมวลผลและบันทึกไฟล์ภาพ (PNG)           |

**รายละเอียดแต่ละ package:**
- **Docnet.Core**: ไลบรารีสำหรับอ่านไฟล์ PDF และแปลงแต่ละหน้าเป็นภาพ bitmap ได้อย่างรวดเร็ว เหมาะกับงาน OCR ที่ต้องแยกหน้า PDF
- **Microsoft.Extensions.Configuration**: ไลบรารีมาตรฐานของ .NET สำหรับโหลดคอนฟิก เช่น API Key, Endpoint ฯลฯ
- **Microsoft.Extensions.Configuration.Json**: ส่วนเสริมที่ช่วยให้ .NET โหลดคอนฟิกจากไฟล์ JSON ได้ง่าย (ใช้กับ appsettings.json)
- **SkiaSharp**: ไลบรารีกราฟิก cross-platform สำหรับสร้าง/บันทึก/แปลงไฟล์ภาพ เช่น PNG, JPEG ฯลฯ ใช้ร่วมกับ Docnet.Core เพื่อบันทึกภาพแต่ละหน้าของ PDF

## วิธีการใช้งาน

1. ติดตั้ง .NET 8.0 หรือเวอร์ชันที่รองรับ
   - สามารถดาวน์โหลดและติดตั้ง .NET 8.0 ได้จากเว็บไซต์ทางการของไมโครซอฟท์: https://dotnet.microsoft.com/download/dotnet/8.0
   - หลังติดตั้ง ตรวจสอบเวอร์ชันด้วยคำสั่ง `dotnet --version`
2. สั่ง build โปรเจกต์

   ```sh
   dotnet build
   ```

3. รันโปรแกรม (ควรรันในโฟลเดอร์ POCMistralOCR หรือระบุ path ของโปรเจกต์ย่อยให้ถูกต้อง)
   - ตัวอย่าง:

     ```sh
     cd POCMistralOCR
     dotnet run
     ```

   - หรือจาก root folder:

     ```sh
     dotnet run --project POCMistralOCR/POCMistralOCR.csproj
     ```

   - **หากใช้ Visual Studio Code สามารถรันหรือ build โครงการได้ง่าย ๆ ด้วย VS Code Tasks:**
     - เปิด Command Palette (กด `F1` หรือ `Cmd+Shift+P`)
     - เลือก `Tasks: Run Task`
     - เลือก `Build OcrConsoleApp` เพื่อ build หรือ `Run OcrConsoleApp` เพื่อรันโปรแกรม

4. นำไฟล์ PDF หรือรูปภาพที่ต้องการแปลงไปไว้ในโฟลเดอร์ `docs/input/`
5. ผลลัพธ์จะถูกบันทึกไว้ในโฟลเดอร์ `docs/output/`

### โฟลเดอร์ input และ output

- **docs/input/**
  - ใช้สำหรับวางไฟล์ PDF หรือรูปภาพที่ต้องการแปลงข้อความ เช่น `1.Scanned.pdf`, `2.TablePure.pdf` เป็นต้น
  - รองรับไฟล์ PDF หลายไฟล์ สามารถนำไฟล์ที่ต้องการประมวลผลมาใส่ไว้ในโฟลเดอร์นี้ได้เลย

- **docs/output/**
  - จะสร้างโฟลเดอร์ย่อยตามชื่อไฟล์ต้นฉบับ เช่น `1.Scanned/`, `2.TablePure/` เป็นต้น
  - ในแต่ละโฟลเดอร์ย่อย (เช่น `1.Scanned/`) จะมีโฟลเดอร์ย่อยหลัก ๆ คือ
    - `0.SplitPdfToImages/` : เก็บไฟล์ภาพ (.png) ที่แยกออกมาจากแต่ละหน้า PDF
    - `1.MistralOCR/` : ภายในจะมีโฟลเดอร์ย่อยแยกตามหน้า เช่น `page-1/`, `page-2/` ...
      - ในแต่ละโฟลเดอร์ page-N จะมีไฟล์ผลลัพธ์ เช่น
        - `page-N.md` (ไฟล์ข้อความ Markdown ที่ได้จาก OCR)
        - `img-0.jpeg` (หรือไฟล์ภาพอื่น ๆ ที่เกี่ยวข้องกับหน้านั้น)
  - ตัวอย่างเช่น หากมีไฟล์ `4.TextWithTableWithImage.pdf` ใน docs/input จะได้โครงสร้างดังนี้:

```text
docs/
├── input/
│   ├── 4.TextWithTableWithImage.pdf
│   └── ...
└── output/
    └── 4.TextWithTableWithImage/
        ├── 0.SplitPdfToImages/
        │   ├── 4.TextWithTableWithImage-1.png
        │   └── ...
        └── 1.MistralOCR/
            ├── page-1/
            │   ├── page-1.md
            │   └── img-0.jpeg
            ├── page-2/
            │   ├── page-2.md
            │   └── ...
            └── ...
```

- docs/output จะจัดเก็บผลลัพธ์แยกตามไฟล์ต้นฉบับ โดยแต่ละไฟล์จะมีโฟลเดอร์สำหรับภาพที่แยกจาก PDF และโฟลเดอร์สำหรับผลลัพธ์ OCR ที่แยกตามหน้า

#### โครงสร้างไฟล์ใน docs/input และ docs/output (โดยสังเขป)

- **docs/input/**
  - วางไฟล์ PDF หรือรูปภาพที่ต้องการแปลง เช่น
    - 1.Scanned.pdf
    - 2.TablePure.pdf
    - 3.Image.pdf
    - 4.TextWithTableWithImage.pdf

- **docs/output/**
  - จะมีโฟลเดอร์ย่อยตามชื่อไฟล์ต้นฉบับ เช่น
    - 1.Scanned/
    - 2.TablePure/
    - 3.Image/
    - 4.TextWithTableWithImage/
  - ในแต่ละโฟลเดอร์ย่อยจะประกอบด้วย:
    - 0.SplitPdfToImages/   (เก็บไฟล์ .png ที่แยกจากแต่ละหน้า PDF)
    - 1.MistralOCR/   (แยกโฟลเดอร์ page-N สำหรับแต่ละหน้า เก็บ .md และภาพที่เกี่ยวข้อง)

## การตั้งค่า

สามารถแก้ไขค่าต่าง ๆ ได้ในไฟล์ `POCMistralOCR/appsettings.json`

## License

MIT License