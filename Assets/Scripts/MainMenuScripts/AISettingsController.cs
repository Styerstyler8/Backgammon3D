using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Class is used for setting the AI speed, ranging between 10-30 seconds
//The slower the AI, the more time it has to run, therefore the better the moves
public class AISettingsController : MonoBehaviour
{
    private const float slowAISpeed = 30f;
    private const float mediumAISpeed = 20f;
    private const float fastAISpeed = 10f;

    private const string slowAISpeedText = "SLOW";
    private const string mediumAISpeedText = "MEDIUM";
    private const string fastAISpeedText = "FAST";

    [SerializeField] Text AISpeedText;
    [SerializeField] Slider AISpeedSlider;

    //This is called when the AI speed slider is moved and indicates a value has been changed
    //AI speed ranges from 0 (slow) to 2 (fast)
    public void onAISpeedSliderChange() {
        int newAISpeed = (int)AISpeedSlider.value;

        if(newAISpeed == 0) {
            AISpeedText.text = slowAISpeedText;
            MonteCarloAI.allocatedTime = slowAISpeed; //Calls the static float variable allocatedTime and sets a time stop limit to AI run time per turn
        }
        else if(newAISpeed == 1) {
            AISpeedText.text = mediumAISpeedText;
            MonteCarloAI.allocatedTime = mediumAISpeed;
        }
        else {
            AISpeedText.text = fastAISpeedText;
            MonteCarloAI.allocatedTime = fastAISpeed;
        }
    }
}
