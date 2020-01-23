using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class deals with the individual pointe, where it should be able to add/remove checkers and indicate it can be moved to
public class PointeManager : MonoBehaviour {
    private const int MAXCHECKERROW = 6; //Support up to 6 checkers until checkers go on top of one another
    private const int MAXCHECKERS = 15; //Max checkers any one color can have

    //Deals with applying a shader to indicate the pointe is clickable/not clickable
    private const string HIGHLIGHTSHADERNAME = "Shader Graphs/PointeHighlight";
    private const string DEFAULTSHADERNAME = "Lightweight Render Pipeline/Lit";
    private Renderer renderer = null;
    private bool clickable = false;

    //Will hold the position of where the first checker will go
    private Vector3 initialCheckerPos = Vector3.zero;

    private int currCheckerPos = 0; //Points to the next available space a checker can go for the container array
    private int pointePos = -1; //Refers to the numerical representation of the pointe ex. pointe 0
    private Checker[] checkers = new Checker[MAXCHECKERS];

    //This helps with spacing and is board dependent, makes the checkers line up nicely
    [SerializeField] float initialcheckerXOffset = 0.5f;
    [SerializeField] float initialcheckerYOffset = 0.09f;
    [SerializeField] float initialcheckerZOffset = -0.5f;

    //Checker dependent, indicates the amount of distance between checkers on a given pointe
    [SerializeField] float xChangeBetweenCheckers = 1f;
    [SerializeField] float yChangeAboveCheckers = 0.21f;

    //Deals with scaling the pointe, so it is easier to click when it can be clicked
    [SerializeField] float defaultYScale = 1f;
    [SerializeField] float expandedYScale = 1.4f;

    //On game start, this determines where to place a checker relative to in game position
    private void Awake() {
        float pointePosX = this.transform.position.x;
        
        float initialCheckerPosX = 0f;

        if (pointePosX > 0) {
            initialCheckerPosX = pointePosX - initialcheckerXOffset;
        }
        else {
            initialCheckerPosX = pointePosX + initialcheckerXOffset;
        }

        float initialCheckerPosY = this.transform.position.y + initialcheckerYOffset;

        float initialCheckerPosZ = this.transform.position.z + initialcheckerZOffset;

        initialCheckerPos = new Vector3(initialCheckerPosX, initialCheckerPosY, initialCheckerPosZ);

        renderer = GetComponent<Renderer>();
    }

    //Adds a given checker to the pointe, calculating the correct position for it to move to
    public void addChecker(Checker checker) {
        checkers[currCheckerPos] = checker;

        float xOffset = xChangeBetweenCheckers * (currCheckerPos % MAXCHECKERROW);
        float newCheckerX = 0f;

        if(initialCheckerPos.x > 0) {
            newCheckerX = initialCheckerPos.x - xOffset;
        }
        else {
            newCheckerX = initialCheckerPos.x + xOffset;
        }

        float yOffset = 0f;

        if(currCheckerPos >= (2 * MAXCHECKERROW)) {
            yOffset = yChangeAboveCheckers * 2;
        }
        else if(currCheckerPos >= MAXCHECKERROW) {
            yOffset = yChangeAboveCheckers;
        }

        float newCheckerY = initialCheckerPos.y + yOffset;

        checker.transform.position = new Vector3(newCheckerX, newCheckerY, initialCheckerPos.z);

        currCheckerPos++;
    }

    //Returns the last checker the pointe contains and gets rid of it
    public Checker removeChecker() {
        currCheckerPos--;
        Checker removedChecker = checkers[currCheckerPos];
        checkers[currCheckerPos] = null;

        return removedChecker;
    }

    //When the pointe is highlighted, its size is expanded so it is easier to click
    //Also, indicates the given pointe can be moved to
    public void changeHighlightPointe(bool toHighlight) {
        if (toHighlight) {
            renderer.material.shader = Shader.Find(HIGHLIGHTSHADERNAME);
            this.transform.localScale = new Vector3(this.transform.localScale.x, expandedYScale, this.transform.localScale.z);
            clickable = true;
        }
        else {
            renderer.material.shader = Shader.Find(DEFAULTSHADERNAME);
            this.transform.localScale = new Vector3(this.transform.localScale.x, defaultYScale, this.transform.localScale.z);
            clickable = false;
        }
    }

    //Returns whether the pointe is currently able to be moved to
    public bool isClickable() {
        return clickable;
    }

    //Highlights the last checker in the current pointe to indicate that it can be moved
    public void highlightLastChecker(bool toHighlight) {
        checkers[currCheckerPos - 1].changeHighlightChecker(true);
    }

    //Each pointe has a numerical number given to it, starting with pointe 0 and ending with pointe 23
    public void setPointePos(int pos) {
        pointePos = pos;
    }

    //Returns the assigned pointe pos, where pointe 0 is the bottom right and pointe 23 is the top right
    public int getPos() {
        if (pointePos == -1) {
            //This should not occur, or else the pointes were not properly assigned the correct positions
            Debug.Log("Error: Should not access pointe without assigned pointe position");
        }

        return pointePos;
    }
}
