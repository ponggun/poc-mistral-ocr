namespace OcrConsoleApp;

public class MistralOcrResponse {
    public List<MistralOcrPage>? pages { get; set; }
}
public class MistralOcrPage {
    public int index { get; set; }
    public string? markdown { get; set; }
    public List<MistralOcrImage>? images { get; set; }
}
public class MistralOcrImage {
    public string? id { get; set; }
    public string? image_base64 { get; set; }
}
