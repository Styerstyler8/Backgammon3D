using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This class focuses on managing the checkers on the bar and and deals with adding/removing/allowing checkers to be moveable
public class BarMananger : MonoBehaviour
{
    private const int MAXCHECKERS = 15;

    //Stores the positions of where to add checkers
    [SerializeField] Vector3 whiteCheckerPos = Vector3.zero;
    [SerializeField] Vector3 blackCheckerPos = Vector3.zero;

    //Creates an offset between each checker added to the bar
    [SerializeField] float checkerYOffset = 0.5f;

    private Checker[] whiteCheckers = new Checker[MAXCHECKERS];
    private Checker[] blackCheckers = new Checker[MAXCHECKERS];

    private bool whiteOnBar = false;
    private bool blackOnBar = false;

    //Acts like a stack, where it will point to the highest checker on the checker bar stack, so that one will be moved first
    private int currWhitePos = 0;
    private int currBlackPos = 0;

    //Add checker to the bar given a checker. Must be added to the array for future reference and calculates the position to move the checker to
    public void addCheckerOnBar(Checker checker) {
        if(checker.isCheckerWhite()) {
            whiteCheckers[currWhitePos] = checker;

            float adjustedYPos = whiteCheckerPos.y + (checkerYOffset * currWhitePos);
            checker.transform.position = new Vector3(whiteCheckerPos.x, adjustedYPos, whiteCheckerPos.z);

            currWhitePos++;
            whiteOnBar = true;
        }
        else {
            blackCheckers[currBlackPos] = checker;

            float adjustedYPos = blackCheckerPos.y + (checkerYOffset * currBlackPos);
            checker.transform.position = new Vector3(blackCheckerPos.x, adjustedYPos, blackCheckerPos.z);

            currBlackPos++;
            blackOnBar = true;
        }
    }

    //Removes and returns the top most checker from the bar, given which color checker to remove
    public Checker removeCheckerOnBar(bool isWhiteChecker) {
        if(isWhiteChecker) {
            if(whiteOnBar) {
                currWhitePos--;
                Checker removedChecker = whiteCheckers[currWhitePos];
                whiteCheckers[currWhitePos] = null;

                if(currWhitePos == 0) {
                    whiteOnBar = false;
                }

                return removedChecker;
            }
            else {
                //This should not occur, or else it was not properly checked that there exists a checker on the bar. Print a debug and return null
                Debug.LogError("Cannot remove checker from empty bar");
                return null;
            }
        }
        else {
            if(blackOnBar) {
                currBlackPos--;
                Checker removedChecker = blackCheckers[currBlackPos];
                blackCheckers[currBlackPos] = null;

                if (currBlackPos == 0) {
                    blackOnBar = false;
                }

                return removedChecker;
            }
            else {
                //This should not occur, or else it was not properly checked that there exists a checker on the bar. Print a debug and return null
                Debug.LogError("Cannot remove checker from empty bar");
                return null;
            }
        }
    }

    //Applys a shader to the top most checker to indicate it can be chosen
    public void highlightChecker(bool whiteToHighlight) {
        if(whiteToHighlight) {
            whiteCheckers[currWhitePos - 1].changeHighlightChecker(true);
        }
        else {
            blackCheckers[currBlackPos - 1].changeHighlightChecker(true);
        }
    }
}
