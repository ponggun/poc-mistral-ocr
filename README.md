# poc-mistral-ocr

โปรเจกต์นี้เป็นตัวอย่างการใช้งาน OCR (Optical Character Recognition) ร่วมกับโมเดล Mistral สำหรับแปลงไฟล์ PDF หรือรูปภาพให้เป็นข้อความ

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

## วิธีการใช้งาน

1. ติดตั้ง .NET 8.0 หรือเวอร์ชันที่รองรับ
   - สามารถดาวน์โหลดและติดตั้ง .NET 8.0 ได้จากเว็บไซต์ทางการของไมโครซอฟท์: https://dotnet.microsoft.com/download/dotnet/8.0
   - หลังติดตั้ง ตรวจสอบเวอร์ชันด้วยคำสั่ง `dotnet --version`
2. สั่ง build โปรเจกต์

   ```sh
   dotnet build
   ```

3. รันโปรแกรม

   ```sh
   dotnet run --project POCMistralOCR
   ```

4. นำไฟล์ PDF หรือรูปภาพที่ต้องการแปลงไปไว้ในโฟลเดอร์ `docs/input/`
5. ผลลัพธ์จะถูกบันทึกไว้ในโฟลเดอร์ `docs/output/`

## โฟลเดอร์ input และ output

- **docs/input/**
  - ใช้สำหรับวางไฟล์ PDF หรือรูปภาพที่ต้องการแปลงข้อความ เช่น `1.Scanned.pdf`, `2.TablePure.pdf` เป็นต้น
  - รองรับไฟล์ PDF หลายไฟล์ สามารถนำไฟล์ที่ต้องการประมวลผลมาใส่ไว้ในโฟลเดอร์นี้ได้เลย

- **docs/output/**
  - จะสร้างโฟลเดอร์ย่อยตามชื่อไฟล์ต้นฉบับ เช่น `1.Scanned/`, `2.TablePure/` เป็นต้น
  - ในแต่ละโฟลเดอร์ย่อยจะมีโฟลเดอร์ย่อยอีก เช่น
    - `0.SplitPdfToImages/` : เก็บไฟล์ภาพที่แยกออกมาจากแต่ละหน้า PDF (เช่น .png)
    - `1.MistralOCR/markdown/` : เก็บไฟล์ผลลัพธ์ที่ได้จากการ OCR ในรูปแบบ Markdown (.md)
  - สามารถนำผลลัพธ์เหล่านี้ไปใช้งานต่อได้ตามต้องการ

### วิธีใช้งาน input/output

1. นำไฟล์ PDF หรือรูปภาพที่ต้องการแปลงไปไว้ใน `docs/input/`
2. รันโปรแกรมตามขั้นตอนในหัวข้อ "วิธีการใช้งาน"
3. ตรวจสอบผลลัพธ์ที่ได้ใน `docs/output/` ซึ่งจะแยกตามชื่อไฟล์ต้นฉบับและมีโครงสร้างโฟลเดอร์ย่อยสำหรับแต่ละขั้นตอน

## โครงสร้างไฟล์ใน docs/input และ docs/output (โดยสังเขป)

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
    - 1.MistralOCR/markdown/   (เก็บไฟล์ .md ผลลัพธ์จาก OCR)

ตัวอย่าง:

```
docs/
├── input/
│   ├── 1.Scanned.pdf
│   ├── 2.TablePure.pdf
│   └── ...
└── output/
    ├── 1.Scanned/
    │   ├── 0.SplitPdfToImages/
    │   │   ├── 1.Scanned-1.png
    │   │   └── ...
    │   └── 1.MistralOCR/
    │       └── markdown/
    │           ├── 1.Scanned-1.md
    │           └── ...
    └── ...
```

## การตั้งค่า

สามารถแก้ไขค่าต่าง ๆ ได้ในไฟล์ `POCMistralOCR/appsettings.json`

## การใช้งานและขอ Access Token สำหรับ Mistral OCR

- สามารถศึกษาข้อมูลเกี่ยวกับ Mistral OCR ได้ที่เว็บไซต์ทางการ: https://docs.mistral.ai/capabilities/OCR/basic_ocr/
- การขอ Access Token (API Key):
  1. สมัครสมาชิกหรือเข้าสู่ระบบที่ https://console.mistral.ai/
  2. ไปที่เมนู API Keys แล้วสร้าง API Key ใหม่
  3. นำ API Key ที่ได้ไปใส่ในไฟล์ `POCMistralOCR/appsettings.json` ในส่วน `MistralOCR:ApiKey`

## License

MIT License