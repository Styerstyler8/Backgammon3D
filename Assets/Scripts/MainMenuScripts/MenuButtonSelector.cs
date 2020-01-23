using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Class is used for managing what to do when one of the main buttons are clicked
public class MenuButtonSelector : MonoBehaviour
{
    //Main title screen background image source https://www.microsoft.com/en-us/p/backgammon-v/9nblggh4p0jn?activetab=pivot:overviewtab
    private const int gameSceneIndex = 1; //This index stores the index to load the game scene to play. Since only 2 scenes will always be 1, unless the build order is changed.

    public void onLocalPlayButtonSelected() {
        BoardManagerV2.againstAI = false;
        SceneManager.LoadScene(gameSceneIndex);
    }

    public void onAIButtonSelected() {
        BoardManagerV2.againstAI = true; //Calls the BoardManager class's static variable to indicate an AI will be played
        SceneManager.LoadScene(gameSceneIndex); //Loads the next scene, which is the Backgammon game scene
    }

    public void onQuitButtonSelected() {
        Application.Quit(); //Quits the application and shuts down game
    }

}
