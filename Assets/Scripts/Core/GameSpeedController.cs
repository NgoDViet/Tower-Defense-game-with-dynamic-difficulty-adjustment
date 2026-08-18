using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSpeedController : MonoBehaviour
{
    [Header("Nút")]
    public Button speedButton;          // Nút đổi tốc độ x1 → x2 → x3
    public Button stopButton;           // Nút Stop (bật/tắt)

    [Header("Text")]
    public TextMeshProUGUI speedText;   // Text của nút tốc độ
    public TextMeshProUGUI stopText;    // Text của nút Stop (nếu muốn đổi chữ)

    private float[] speeds = { 1f, 2f, 3f };
    private int currentIndex = 0;
    private bool isStopped = false;

    void Start()
    {
        if (speedButton != null)
            speedButton.onClick.AddListener(CycleSpeed);

        if (stopButton != null)
            stopButton.onClick.AddListener(ToggleStop);

        // Khởi tạo
        UpdateSpeedDisplay();
        Time.timeScale = speeds[currentIndex];
    }

    // Nút tốc độ: x1 → x2 → x3 → x1...
    public void CycleSpeed()
    {
        if (isStopped) return; // Đang dừng thì không đổi tốc độ

        currentIndex++;
        if (currentIndex >= speeds.Length)
            currentIndex = 0;

        Time.timeScale = speeds[currentIndex];
        UpdateSpeedDisplay();
    }

    // Nút Stop: bấm lần 1 dừng, lần 2 chạy tiếp, lần 3 dừng...
    public void ToggleStop()
    {
        isStopped = !isStopped;

        if (isStopped)
        {
            Time.timeScale = 0f;
            if (stopText != null) stopText.text = "RESUME";
            Debug.Log("Đã dừng");
        }
        else
        {
            Time.timeScale = speeds[currentIndex];
            if (stopText != null) stopText.text = "STOP";
            Debug.Log("Tiếp tục chạy");
        }
    }

    void UpdateSpeedDisplay()
    {
        if (speedText != null)
            speedText.text = "x" + speeds[currentIndex];
    }

    // Dùng cho nút Resume trong Pause Menu
    public void ResumeToCurrentSpeed()
    {
        isStopped = false;
        Time.timeScale = speeds[currentIndex];
        if (stopText != null) stopText.text = "STOP";
    }
}