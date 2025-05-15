using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DroneInputManager : MonoBehaviour
{
    public TMP_InputField inputKronus;
    public TMP_InputField inputLyrion;
    public TMP_InputField inputMystara;
    public TMP_InputField inputEclipsia;
    public TMP_InputField inputFiora;

    public TextMeshProUGUI errorText;
    public TextMeshProUGUI dronesLeftText;

    private int maxDrones = 999;

    public void ValidateInputs()
    {
        int kronus = ParseInput(inputKronus.text);
        int lyrion = ParseInput(inputLyrion.text);
        int mystara = ParseInput(inputMystara.text);
        int eclipsia = ParseInput(inputEclipsia.text);
        int fiora = ParseInput(inputFiora.text);

        if (!(kronus >= lyrion && lyrion >= mystara && mystara >= eclipsia && eclipsia >= fiora))
        {
            errorText.text = "Значення повинні спадати: Kronus ≥ Lyrion ≥ Mystara ≥ Eclipsia ≥ Fiora";
            dronesLeftText.text = "";
            return;
        }

        int sum = kronus + lyrion + mystara + eclipsia + fiora;
        if (sum > maxDrones)
        {
            errorText.text = $"Ви ввели забагато дронів! Максимум {maxDrones}.";
            dronesLeftText.text = "";
            return;
        }

        errorText.text = "";
        dronesLeftText.text = $"Дронів залишилось: {maxDrones - sum}";
    }

    public void ClearMessages()
    {
        errorText.text = "";
        dronesLeftText.text = "";
    }

    private int ParseInput(string text)
    {
        if (int.TryParse(text, out int val))
            return val;
        return 0;
    }
}