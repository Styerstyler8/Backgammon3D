using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class focuses on how to position added checkers to the barring off location
public class BaringOffManager : MonoBehaviour
{
    //These constants refer to the shader to apply when indicating a checker can be moved here or not
    private const string HIGHLIGHTSHADERNAME = "Shader Graphs/PointeHighlight";
    private const string DEFAULTSHADERNAME = "Lightweight Render Pipeline/Lit";

    //The in game positions where the checkers will stack when barred off
    [SerializeField] Vector3 whiteCheckerPos = Vector3.zero;
    [SerializeField] Vector3 blackCheckerPos = Vector3.zero;

    //The offset between each stacked checker aka 0.5f distance on the y-axis between two checkers when added
    [SerializeField] float checkerYOffset = 0.5f;

    //Renderer is used to apply shader to indicate a checker can go into the barring off location
    [SerializeField] Renderer renderer = null;

    //This will keep track of number of checkers that have barred off. Useful for calculating y offset location
    private int currWhitePos = 0;
    private int currBlackPos = 0;

    //Clickable will indicate if a checker can go here or not, used by boardManager
    private bool clickable = false;

    //When given a checker, this will move the given checker onto the correct checker stack of barred off checkers
    public void addCheckerBaringOff(Checker checker) {
        if (checker.isCheckerWhite()) {
            float adjustedYPos = whiteCheckerPos.y + (checkerYOffset * currWhitePos);
            checker.transform.position = new Vector3(whiteCheckerPos.x, adjustedYPos, whiteCheckerPos.z);

            currWhitePos++;
        }
        else {
            float adjustedYPos = blackCheckerPos.y + (checkerYOffset * currBlackPos);
            checker.transform.position = new Vector3(blackCheckerPos.x, adjustedYPos, blackCheckerPos.z);

            currBlackPos++;
        }
    }

    //Used to change if the barring off location should be a moveable position
    public void changeHighlightBaringOff(bool toHighlight) {
        if (toHighlight) {
            renderer.material.shader = Shader.Find(HIGHLIGHTSHADERNAME);
            clickable = true;
        }
        else {
            renderer.material.shader = Shader.Find(DEFAULTSHADERNAME);
            clickable = false;
        }
    }

    //Returns to board manager if the checker can be moved to the barring off location
    public bool isClickable() {
        return clickable;
    }
}
