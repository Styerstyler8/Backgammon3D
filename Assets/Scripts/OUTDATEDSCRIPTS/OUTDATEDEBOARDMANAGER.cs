//OUTDATED
//USE FOR REFERENCE ONLY
/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{

    //Player is white checkers
    private const int MAXPOINTES = 24;
    private const int MAXCHECKERS = 15;

    [SerializeField] PointeManager[] pointes = new PointeManager[MAXPOINTES];
    [SerializeField] Checker[] whiteCheckers = new Checker[MAXCHECKERS];
    [SerializeField] Checker[] blackCheckers = new Checker[MAXCHECKERS];
    [SerializeField] DiceManager dice = null;
    [SerializeField] BarMananger bar = null;
    [SerializeField] BaringOffManager baringOff = null;
    [SerializeField] float waitForDiceRollTime = 2f;
    [SerializeField] float selectedPieceYOffset = 2f;

    private enum turnPhase { StartGame, RollDice, WhiteTurn, BlackTurn };
    private enum gamePhase { Regular, BaringOff };

    private turnPhase currGamePhase;
    private gamePhase whiteCheckerPhase;
    private gamePhase blackCheckerPhase;

    private int movesLeftForTurn = -1;
    private bool usedRoll1 = false;
    private bool usedRoll2 = false;

    private ArrayList highlightedPointes = new ArrayList();

    private Vector3 selectedCheckerOrigPos = new Vector3(0f, 0f, 0f);
    private Checker selectedChecker = null;
    private int selectedCheckerOrigBoardPos = -1;

    private bool canUseBothMoves = true;

    private int[] rolledValues = new int[2];

    private int[] board = new int[MAXPOINTES + 4];

    /* board[23] is last pointe
     * board[24-25] is whiteBar and blackBar respectively
     * board[26-27] is white checkers bared off and black checkers bared off
     *

    private void Start() {
        dice.resetDicePos();
        arrangeBoard();

        currGamePhase = turnPhase.StartGame;
        whiteCheckerPhase = gamePhase.Regular;
        blackCheckerPhase = gamePhase.Regular;

        Debug.Log("Determining who goes first (player is 1st dice):");
        StartCoroutine(determinePlayerOrder());
    }

    IEnumerator determinePlayerOrder() {
        rolledValues = dice.throwDice(false);

        yield return new WaitForSeconds(waitForDiceRollTime);

        if (rolledValues[0] > rolledValues[1]) {
            Debug.Log("white goes first...");
            currGamePhase = turnPhase.WhiteTurn;
        }
        else {
            Debug.Log("black goes first...");
            currGamePhase = turnPhase.BlackTurn;
        }

        movesLeftForTurn = 2;
        canUseBothMoves = true;
        highlightCheckersMoveable(rolledValues[0]);
        highlightCheckersMoveable(rolledValues[1]);
    }

    //Returns true if move possible, false if no move possible
    private bool highlightCheckersMoveable(int numRolled) {
        if(currGamePhase == turnPhase.WhiteTurn) {
            //Consider white checkers that can be moved with num
            if(whiteCheckerPhase == gamePhase.BaringOff) {
                return checkWhiteBarringOff(numRolled, true);
            }
            else {
                //Regular rules
                if(board[24] > 0) {
                    return checkWhiteOnBar(numRolled, true);
                }
                else {
                    //Consider entire board of possible moves
                    return checkWhiteRegular(numRolled, true);
                }
            }
        }
        else {
            //Consider black checkers that can be moved with num
            if(blackCheckerPhase == gamePhase.BaringOff) {
                return checkBlackBarringOff(numRolled, true);
            }
            else {
                //Regular rules
                if (board[25] > 0) {
                    return checkBlackOnBar(numRolled, true);
                }
                else {
                    //Consider entire board of possible moves
                    return checkBlackRegular(numRolled, true);
                }
            }
        }
    }

    private void refineHighlightedCheckers(int useableNum, int unuseableNum) {
        //One case to worry about, where a double move is required but multiple single moves available
        //Must restrict highlighted checker to only the one that can double move
        //Only happens when 1 move can occur and the other cannot and both moves havent been used

        if (currGamePhase == turnPhase.WhiteTurn) {
            if (whiteCheckerPhase == gamePhase.BaringOff || board[24] > 0) {
                return;
            }

            bool foundNewGoodChecker = false;
            ArrayList checkersToHighlight = new ArrayList();
            for (int i = (useableNum + unuseableNum); i < MAXPOINTES; i++) {
                if (board[i] > 0 && board[i - useableNum] > -2 && board[i - useableNum - unuseableNum] > -2) {
                    checkersToHighlight.Add(pointes[i]);
                    foundNewGoodChecker = true;
                }
            }

            if (foundNewGoodChecker) {
                removeCheckerHighlights();
                for(int i = 0; i < checkersToHighlight.Count; i++) {
                    PointeManager currPointe = (PointeManager)checkersToHighlight[i];
                    currPointe.highlightLastChecker(true);
                }
            }
        }
        else {
            if (blackCheckerPhase == gamePhase.BaringOff || board[25] > 0) {
                return;
            }

            bool foundNewGoodChecker = false;
            ArrayList checkersToHighlight = new ArrayList();
            for (int i = (MAXPOINTES - useableNum - unuseableNum - 1); i >= 0; i--) {
                if (board[i] < 0 && board[i + useableNum] < 2 && board[i + useableNum + unuseableNum] < 2) {
                    checkersToHighlight.Add(pointes[i]);
                    foundNewGoodChecker = true;
                }
            }

            if (foundNewGoodChecker) {
                removeCheckerHighlights();
                for (int i = 0; i < checkersToHighlight.Count; i++) {
                    PointeManager currPointe = (PointeManager)checkersToHighlight[i];
                    currPointe.highlightLastChecker(true);
                }
            }
        }
    }

    private void removeCheckerHighlights() {
        for(int i = 0; i < MAXCHECKERS; i++) {
            whiteCheckers[i].changeHighlightChecker(false);
            blackCheckers[i].changeHighlightChecker(false);
        }
    }

    private bool checkWhiteBarringOff(int numRolled, bool showHighlight) {
        //Baring off rules
        if (board[numRolled - 1] > 0) {
            //Highlight checker on pointe indicated by numberRolled
            if (showHighlight) {
                pointes[numRolled - 1].highlightLastChecker(true);
            }
        }
        else {
            //Check higher numbered pointes for valid checkers, else highlight next lowest pointe checker
            int currCheckPos = 5;
            bool highlightedCheckerAlready = false;
            while (currCheckPos > (numRolled - 1)) {
                if (board[currCheckPos] > 0 && board[currCheckPos - numRolled] >= -1) {
                    if (showHighlight) {
                        pointes[currCheckPos].highlightLastChecker(true);
                    }

                    highlightedCheckerAlready = true;
                }

                currCheckPos--;
            }

            if (!highlightedCheckerAlready) {
                currCheckPos = numRolled - 1;
                while (currCheckPos >= 0 && !highlightedCheckerAlready) {
                    if (board[currCheckPos] > 0) {
                        if (showHighlight) {
                            pointes[currCheckPos].highlightLastChecker(true);
                        }

                        highlightedCheckerAlready = true;
                    }

                    currCheckPos--;
                }

                if (!highlightedCheckerAlready) {
                    Debug.Log("No playable moves");
                    return false;
                }
            }
        }

        return true;
    }

    private bool checkBlackBarringOff(int numRolled, bool showHighlight) {
        //Baring off rules
        if (board[MAXPOINTES - numRolled] < 0) {
            //Highlight checker on pointe indicated by numberRolled
            if(showHighlight) {
                pointes[MAXPOINTES - numRolled].highlightLastChecker(true);
            }
        }
        else {
            //Check higher numbered pointes for valid checkers, else highlight next lowest pointe checker
            int currCheckPos = 18;
            bool highlightedCheckerAlready = false;
            while (currCheckPos < (MAXPOINTES - numRolled)) {
                if (board[currCheckPos] < 0 && board[currCheckPos + numRolled] <= 1) {
                    if (showHighlight) {
                        pointes[currCheckPos].highlightLastChecker(true);
                    }

                    highlightedCheckerAlready = true;
                }

                currCheckPos++;
            }

            if (!highlightedCheckerAlready) {
                currCheckPos = MAXPOINTES - numRolled;
                while (currCheckPos < MAXPOINTES && !highlightedCheckerAlready) {
                    if (board[currCheckPos] < 0) {
                        if (showHighlight) {
                            pointes[currCheckPos].highlightLastChecker(true);
                        }

                        highlightedCheckerAlready = true;
                    }

                    currCheckPos++;
                }

                if (!highlightedCheckerAlready) {
                    Debug.Log("No playable moves");
                    return false;
                }
            }
        }

        return true;
    }

    private bool checkWhiteOnBar(int numRolled, bool showHighlight) {
        //White checker on bar then, must move off
        if (board[MAXPOINTES - numRolled] > -2) {
            if (showHighlight) {
                bar.highlightChecker(true);
            }

            return true;
        }

        return false;
    }

    private bool checkBlackOnBar(int numRolled, bool showHighlight) {
        //Black checker on bar then, must move off
        if (board[numRolled - 1] < 2) {
            if (showHighlight) {
                bar.highlightChecker(false);
            }

            return true;
        }

        return false;
    }

    private bool checkWhiteRegular(int numRolled, bool showHighlight) {
        //Not baring off so lowest pointe to consider for white is pointe at numRolled
        bool foundCheckerToHighlight = false;
        for (int i = numRolled; i < MAXPOINTES; i++) {
            if (board[i] > 0 && board[i - numRolled] > -2) {
                if (showHighlight) {
                    pointes[i].highlightLastChecker(true);
                }

                foundCheckerToHighlight = true;
            }
        }

        if (!foundCheckerToHighlight) {
            return false;
        }

        return true;
    }

    private bool checkBlackRegular(int numRolled, bool showHighlight) {
        //Not baring off so lowest pointe to consider for black is pointe at MAXPOINTES - numRolled
        bool foundCheckerToHighlight = false;
        for (int i = (MAXPOINTES - numRolled - 1); i >= 0; i--) {
            if (board[i] < 0 && board[i + numRolled] < 2) {
                if(showHighlight) {
                    pointes[i].highlightLastChecker(true);
                }
                
                foundCheckerToHighlight = true;
            }
        }

        if (!foundCheckerToHighlight) {
            return false;
        }

        return true;
    }

    private void arrangeBoard() {
        pointes[0].addChecker(blackCheckers[0]);
        pointes[0].addChecker(blackCheckers[1]);
        board[0] = -2;

        pointes[5].addChecker(whiteCheckers[0]);
        pointes[5].addChecker(whiteCheckers[1]);
        pointes[5].addChecker(whiteCheckers[2]);
        pointes[5].addChecker(whiteCheckers[3]);
        pointes[5].addChecker(whiteCheckers[4]);
        board[5] = 5;

        pointes[7].addChecker(whiteCheckers[5]);
        pointes[7].addChecker(whiteCheckers[6]);
        pointes[7].addChecker(whiteCheckers[7]);
        board[7] = 3;

        pointes[11].addChecker(blackCheckers[2]);
        pointes[11].addChecker(blackCheckers[3]);
        pointes[11].addChecker(blackCheckers[4]);
        pointes[11].addChecker(blackCheckers[5]);
        pointes[11].addChecker(blackCheckers[6]);
        board[11] = -5;

        pointes[12].addChecker(whiteCheckers[8]);
        pointes[12].addChecker(whiteCheckers[9]);
        pointes[12].addChecker(whiteCheckers[10]);
        pointes[12].addChecker(whiteCheckers[11]);
        pointes[12].addChecker(whiteCheckers[12]);
        board[12] = 5;

        pointes[16].addChecker(blackCheckers[7]);
        pointes[16].addChecker(blackCheckers[8]);
        pointes[16].addChecker(blackCheckers[9]);
        board[16] = -3;

        pointes[18].addChecker(blackCheckers[10]);
        pointes[18].addChecker(blackCheckers[11]);
        pointes[18].addChecker(blackCheckers[12]);
        pointes[18].addChecker(blackCheckers[13]);
        pointes[18].addChecker(blackCheckers[14]);
        board[18] = -5;

        pointes[23].addChecker(whiteCheckers[13]);
        pointes[23].addChecker(whiteCheckers[14]);
        board[23] = 2;
    }

    private void makeNearBarringOffBoard() {
        pointes[1].addChecker(whiteCheckers[0]);
        board[1] = 1;

        pointes[2].addChecker(whiteCheckers[1]);
        pointes[2].addChecker(whiteCheckers[2]);
        board[2] = 2;

        pointes[3].addChecker(blackCheckers[0]);
        pointes[3].addChecker(blackCheckers[1]);
        board[3] = -2;

        pointes[4].addChecker(whiteCheckers[3]);
        pointes[4].addChecker(whiteCheckers[4]);
        pointes[4].addChecker(whiteCheckers[5]);
        pointes[4].addChecker(whiteCheckers[6]);
        pointes[4].addChecker(whiteCheckers[7]);
        board[4] = 5;

        pointes[5].addChecker(whiteCheckers[8]);
        pointes[5].addChecker(whiteCheckers[9]);
        pointes[5].addChecker(whiteCheckers[10]);
        pointes[5].addChecker(whiteCheckers[11]);
        pointes[5].addChecker(whiteCheckers[12]);
        pointes[5].addChecker(whiteCheckers[13]);
        board[5] = 6;

        pointes[7].addChecker(whiteCheckers[14]);
        board[7] = 1;

        pointes[18].addChecker(blackCheckers[2]);
        pointes[18].addChecker(blackCheckers[3]);
        pointes[18].addChecker(blackCheckers[4]);
        pointes[18].addChecker(blackCheckers[5]);
        pointes[18].addChecker(blackCheckers[6]);
        board[18] = -5;

        pointes[19].addChecker(blackCheckers[7]);
        pointes[19].addChecker(blackCheckers[8]);
        board[19] = -2;

        pointes[20].addChecker(blackCheckers[9]);
        board[20] = -1;

        pointes[21].addChecker(blackCheckers[10]);
        board[21] = -1;

        pointes[22].addChecker(blackCheckers[11]);
        pointes[22].addChecker(blackCheckers[12]);
        pointes[22].addChecker(blackCheckers[13]);
        board[22] = -3;

        pointes[23].addChecker(blackCheckers[14]);
        board[23] = -1;
    }

    private void Update() {
        if (Input.GetMouseButtonUp(0) && selectedChecker) {
            RaycastHit[] hits;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            hits = Physics.RaycastAll(ray);
            bool clickedPointe = false;
            int newPointePos = 0;

            for (int i = 0; i < hits.Length; i++) {
                PointeManager pointeSelected = hits[i].transform.GetComponent<PointeManager>();
                BaringOffManager clickedBaringOff = hits[i].transform.GetComponent<BaringOffManager>();

                if (pointeSelected && pointeSelected.isClickable()) {
                    clickedPointe = true;

                    newPointePos = calculateBoardPos(pointeSelected.transform.position, false);

                    //Look at board seperately for white phase vs black
                    if(currGamePhase == turnPhase.WhiteTurn) {
                        //Check for hit and do actions if hit
                        if (board[newPointePos] == -1) {
                            blackCheckerPhase = gamePhase.Regular;
                            bar.addCheckerOnBar(pointes[newPointePos].removeChecker());
                            board[newPointePos] = 0;
                            board[25]++;
                        }

                        if (board[24] > 0) {
                            pointes[newPointePos].addChecker(bar.removeCheckerOnBar(true));
                            selectedCheckerOrigBoardPos = 24;
                            board[24]--;
                        }
                        //Not on bar, then regular move and do actions
                        else {
                            pointes[newPointePos].addChecker(pointes[selectedCheckerOrigBoardPos].removeChecker());
                            board[selectedCheckerOrigBoardPos]--;
                        }

                        board[newPointePos]++;
                    }
                    else if(currGamePhase == turnPhase.BlackTurn) {
                        //Check for hit and do actions if hit
                        if (board[newPointePos] == 1) {
                            whiteCheckerPhase = gamePhase.Regular;
                            bar.addCheckerOnBar(pointes[newPointePos].removeChecker());
                            board[newPointePos] = 0;
                            board[24]++;
                        }

                        if (board[25] > 0) {
                            pointes[newPointePos].addChecker(bar.removeCheckerOnBar(false));
                            selectedCheckerOrigBoardPos = -1;
                            board[25]--;
                        }
                        //Not on bar, then regular move and do actions
                        else {
                            pointes[newPointePos].addChecker(pointes[selectedCheckerOrigBoardPos].removeChecker());
                            board[selectedCheckerOrigBoardPos]++;
                        }

                        board[newPointePos]--;
                    }
                }
                else if (clickedBaringOff && clickedBaringOff.isClickable()) {
                    clickedPointe = true;
                    baringOff.addCheckerBaringOff(pointes[selectedCheckerOrigBoardPos].removeChecker());
                    
                    if(currGamePhase == turnPhase.WhiteTurn) {
                        board[selectedCheckerOrigBoardPos]--;
                        board[26]++;
                        newPointePos = -1;
                    }
                    else {
                        board[selectedCheckerOrigBoardPos]++;
                        board[27]--;
                        newPointePos = 24;
                    }

                    baringOff.changeHighlightBaringOff(false);
                }
            }

            if (clickedPointe) {
                removeCheckerHighlights();
                movesLeftForTurn--;

                if(currGamePhase == turnPhase.WhiteTurn) {
                    if(board[26] == MAXCHECKERS) {
                        Debug.Log("White won");
                        Application.Quit();
                    }

                    if(whiteCanBearOff()) {
                        whiteCheckerPhase = gamePhase.BaringOff;
                        Debug.Log("White is now barring off");
                    }
                }
                else {
                    if (board[27] == MAXCHECKERS) {
                        Debug.Log("Black won");
                        Application.Quit();
                    }

                    if (blackCanBearOff()) {
                        blackCheckerPhase = gamePhase.BaringOff;
                        Debug.Log("Black is now barring off");
                    }
                }

                bool canUseAnotherMove = true;
                if (movesLeftForTurn == 0) {
                    //Switch turns
                    dice.resetDicePos();

                    if (currGamePhase == turnPhase.WhiteTurn) {
                        currGamePhase = turnPhase.BlackTurn;
                    }
                    else {
                        currGamePhase = turnPhase.WhiteTurn;
                    }
                }
                else if (rolledValues[0] == rolledValues[1]) {
                    usedRoll1 = true;
                    canUseAnotherMove = highlightCheckersMoveable(rolledValues[1]);
                }
                else if (Mathf.Abs(newPointePos - selectedCheckerOrigBoardPos) == rolledValues[0]) {
                    usedRoll1 = true;
                    canUseAnotherMove = highlightCheckersMoveable(rolledValues[1]);
                }
                else {
                    usedRoll2 = true;
                    canUseAnotherMove = highlightCheckersMoveable(rolledValues[0]);
                }

                if (!canUseAnotherMove) {
                    //Switch turns
                    dice.resetDicePos();

                    if(currGamePhase == turnPhase.WhiteTurn) {
                        currGamePhase = turnPhase.BlackTurn;
                    }
                    else {
                        currGamePhase = turnPhase.WhiteTurn;
                    }
                }

            }
            else {
                selectedChecker.transform.position = selectedCheckerOrigPos;
            }

            selectedChecker = null;
            selectedCheckerOrigPos = Vector3.zero;
            selectedCheckerOrigBoardPos = -1;

            resetPointes();
        }

        if (selectedChecker) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit)) {
                selectedChecker.transform.position = new Vector3(hit.point.x, selectedPieceYOffset, hit.point.z);
            }
        }

        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit)) {
                Checker clickedChecker = hit.transform.GetComponent<Checker>();
                Dice clickedDice = hit.transform.GetComponent<Dice>();

                if (clickedChecker && clickedChecker.isClickable()) {
                    selectedChecker = clickedChecker;
                    selectedCheckerOrigPos = selectedChecker.transform.position;

                    selectedCheckerOrigBoardPos = calculateBoardPos(selectedCheckerOrigPos, true);

                    highlightPointes(selectedCheckerOrigBoardPos);

                    if(!usedRoll1 && !usedRoll2 && canUseBothMoves && rolledValues[0] != rolledValues[1] && bar.numCheckersOnBar(selectedChecker.isCheckerWhite()) < 2) {
                        refineHighlightedPointes(selectedCheckerOrigBoardPos);
                    }
                }
                else if (clickedDice && clickedDice.isClickable()) {
                    //Conduct dice phase
                    StartCoroutine(rollDice());
                }
                else {
                    Debug.Log("Clicked invalid object");
                }
            }
        }

        if (Input.GetMouseButtonDown(1) && selectedChecker) {
            selectedChecker.transform.position = selectedCheckerOrigPos;
            selectedChecker = null;
            selectedCheckerOrigPos = Vector3.zero;
            selectedCheckerOrigBoardPos = -1;
            resetPointes();
        }
    }

    private void resetPointes() {
        baringOff.changeHighlightBaringOff(false);
        for(int i = 0; i < highlightedPointes.Count; i++) {
            PointeManager currPointe = (PointeManager)highlightedPointes[i];
            currPointe.changeHighlightPointe(false);
        }

        highlightedPointes.Clear();
    }

    private void highlightPointes(int boardPos) {
        if (currGamePhase == turnPhase.WhiteTurn) {
            //Consider white checkers that can be moved with num
            if (whiteCheckerPhase == gamePhase.BaringOff) {
                if(!usedRoll1 && (boardPos - rolledValues[0]) < 0) {
                    baringOff.changeHighlightBaringOff(true);
                }
                //Highlight pointe to move to
                else if(!usedRoll1 && (boardPos - rolledValues[0]) >= 0 && board[boardPos - rolledValues[0]] > -2) {
                    pointes[boardPos - rolledValues[0]].changeHighlightPointe(true);
                    highlightedPointes.Add(pointes[boardPos - rolledValues[0]]);
                }
                if(!usedRoll2 && (boardPos - rolledValues[1]) < 0) {
                    baringOff.changeHighlightBaringOff(true);
                }
                //Highlight pointe to move to
                else if (!usedRoll2 && (boardPos - rolledValues[1]) >= 0 && board[boardPos - rolledValues[1]] > -2) {
                    pointes[boardPos - rolledValues[1]].changeHighlightPointe(true);
                    highlightedPointes.Add(pointes[boardPos - rolledValues[1]]);
                }
            }
            else {
                //Regular rules
                if (board[24] > 0) {
                    if(!usedRoll1 && board[MAXPOINTES - rolledValues[0]] > -2) {
                        pointes[MAXPOINTES - rolledValues[0]].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[MAXPOINTES - rolledValues[0]]);
                    }
                    if(!usedRoll2 && board[MAXPOINTES - rolledValues[1]] > -2) {
                        pointes[MAXPOINTES - rolledValues[1]].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[MAXPOINTES - rolledValues[1]]);
                    }
                }
                else {
                    if (!usedRoll1 && (boardPos - rolledValues[0]) >= 0 && board[boardPos - rolledValues[0]] > -2) {
                        pointes[boardPos - rolledValues[0]].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[boardPos - rolledValues[0]]);
                    }
                    if (!usedRoll2 && (boardPos - rolledValues[1]) >= 0 && board[boardPos - rolledValues[1]] > -2) {
                        pointes[boardPos - rolledValues[1]].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[boardPos - rolledValues[1]]);
                    }
                }
            }
        }
        else {
            //Consider black checkers that can be moved with num
            if (blackCheckerPhase == gamePhase.BaringOff) {
                if (!usedRoll1 && (boardPos + rolledValues[0]) >= MAXPOINTES) {
                    baringOff.changeHighlightBaringOff(true);
                }
                //Highlight pointe to move to
                else if (!usedRoll1 && (boardPos + rolledValues[0]) < MAXPOINTES && board[boardPos + rolledValues[0]] < 2) {
                    pointes[boardPos + rolledValues[0]].changeHighlightPointe(true);
                    highlightedPointes.Add(pointes[boardPos + rolledValues[0]]);
                }
                if (!usedRoll2 && (boardPos + rolledValues[1]) >= MAXPOINTES) {
                    baringOff.changeHighlightBaringOff(true);
                }
                //Highlight pointe to move to
                else if (!usedRoll2 && (boardPos + rolledValues[1]) < MAXPOINTES && board[boardPos + rolledValues[1]] < 2) {
                    pointes[boardPos + rolledValues[1]].changeHighlightPointe(true);
                    highlightedPointes.Add(pointes[boardPos + rolledValues[1]]);
                }
            }
            else {
                //Regular rules
                if (board[25] > 0) {
                    if (!usedRoll1 && board[rolledValues[0] - 1] < 2) {
                        pointes[rolledValues[0] - 1].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[rolledValues[0] - 1]);
                    }
                    if (!usedRoll2 && board[rolledValues[1] - 1] < 2) {
                        pointes[rolledValues[1] - 1].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[rolledValues[1] - 1]);
                    }
                }
                else {
                    if (!usedRoll1 && (boardPos + rolledValues[0]) < MAXPOINTES && board[boardPos + rolledValues[0]] < 2) {
                        pointes[boardPos + rolledValues[0]].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[boardPos + rolledValues[0]]);
                    }
                    if (!usedRoll2 && (boardPos + rolledValues[1]) < MAXPOINTES && board[boardPos + rolledValues[1]] < 2) {
                        pointes[boardPos + rolledValues[1]].changeHighlightPointe(true);
                        highlightedPointes.Add(pointes[boardPos + rolledValues[1]]);
                    }
                }
            }
        }
    }

    private void refineHighlightedPointes(int currCheckerPos) {
        //Cover bar case (1 on bar, check if only 1 of 2 moves possible, where larger move must be used, must check if smaller then larger move results in valid move or if another checker can be moved after)
        //If we do roll 1, can roll 2 then be done, or does 1 have to go then 2, vice versa
        //If we move and then in bearing off phase, can move then be done
        int largerRoll = Mathf.Max(rolledValues[0], rolledValues[1]);
        int smallerRoll = Mathf.Min(rolledValues[0], rolledValues[1]);
        int prevNum;


        if(currGamePhase == turnPhase.WhiteTurn) {
            if(whiteCheckerPhase == gamePhase.BaringOff) {
                if((currCheckerPos - largerRoll) == -1) {
                    pointes[currCheckerPos - smallerRoll].changeHighlightPointe(false);
                }
                else if((currCheckerPos - largerRoll) < 0 && (currCheckerPos - smallerRoll) >= 0) {
                    if(isGreaterWhitePointe(currCheckerPos)) {
                        baringOff.changeHighlightBaringOff(false);
                    }
                }
                if((currCheckerPos - smallerRoll) >= 0 && board[smallerRoll - 1] > 0) {
                    pointes[currCheckerPos - smallerRoll].changeHighlightPointe(false);
                }
                if ((currCheckerPos - largerRoll) >= 0 && board[largerRoll - 1] > 0) {
                    pointes[currCheckerPos - largerRoll].changeHighlightPointe(false);
                }

                return;
            }

            if(board[24] > 0) {
                board[24] = 0;
                prevNum = board[MAXPOINTES - smallerRoll];
                board[MAXPOINTES - smallerRoll] = 1;

                if(!checkWhiteRegular(largerRoll, false)) {
                    //Unhighlight pointe of smaller role, since must use larger roll
                    pointes[MAXPOINTES - smallerRoll].changeHighlightPointe(false);
                }

                board[24] = 1;
                board[MAXPOINTES - smallerRoll] = prevNum;
            }
            else {
                //Must play larger case
                prevNum = board[currCheckerPos - smallerRoll];
                board[currCheckerPos - smallerRoll] = prevNum + 1;
                board[currCheckerPos]--;

                if(whiteCanBearOff()) {
                    whiteCheckerPhase = gamePhase.BaringOff;

                    if(!checkWhiteBarringOff(largerRoll, false)) {
                        //Unhighlight pointe of smaller role, since must use larger roll
                        pointes[currCheckerPos - smallerRoll].changeHighlightPointe(false);
                    }
                }
                else {
                    if (!checkWhiteRegular(largerRoll, false)) {
                        //Unhighlight pointe of smaller role, since must use larger roll
                        pointes[currCheckerPos - smallerRoll].changeHighlightPointe(false);
                    }
                }

                whiteCheckerPhase = gamePhase.Regular;
                board[currCheckerPos - smallerRoll] = prevNum;
                board[currCheckerPos]++;
            }
        }
        else {
            if (blackCheckerPhase == gamePhase.BaringOff) {
                if((currCheckerPos + largerRoll) == MAXPOINTES) {
                    pointes[currCheckerPos + smallerRoll].changeHighlightPointe(false);
                }
                else if ((currCheckerPos + largerRoll) > MAXPOINTES && (currCheckerPos + smallerRoll) < MAXPOINTES) {
                    if (isGreaterBlackPointe(currCheckerPos)) {
                        baringOff.changeHighlightBaringOff(false);
                    }
                }
                if ((currCheckerPos + smallerRoll) < MAXPOINTES && board[MAXPOINTES - smallerRoll] < 0) {
                    pointes[currCheckerPos + smallerRoll].changeHighlightPointe(false);
                }
                if ((currCheckerPos + largerRoll) < MAXPOINTES && board[MAXPOINTES - largerRoll] < 0) {
                    pointes[currCheckerPos + largerRoll].changeHighlightPointe(false);
                }

                return;
            }
            if (board[25] > 0) {
                board[25] = 0;
                prevNum = board[smallerRoll - 1];
                board[smallerRoll - 1] = -1;

                if (!checkBlackRegular(largerRoll, false)) {
                    //Unhighlight pointe of smaller role, since must use larger roll
                    pointes[smallerRoll - 1].changeHighlightPointe(false);
                }

                board[25] = 1;
                board[smallerRoll - 1] = prevNum;
            }
            else {
                //Must play larger case
                prevNum = board[currCheckerPos + smallerRoll];
                board[currCheckerPos + smallerRoll] = prevNum - 1;
                board[currCheckerPos]++;

                if (blackCanBearOff()) {
                    blackCheckerPhase = gamePhase.BaringOff;

                    if (!checkBlackBarringOff(largerRoll, false)) {
                        //Unhighlight pointe of smaller role, since must use larger roll
                        pointes[currCheckerPos + smallerRoll].changeHighlightPointe(false);
                    }
                }
                else {
                    if (!checkBlackRegular(largerRoll, false)) {
                        //Unhighlight pointe of smaller role, since must use larger roll
                        pointes[currCheckerPos + smallerRoll].changeHighlightPointe(false);
                    }
                }

                blackCheckerPhase = gamePhase.Regular;
                board[currCheckerPos + smallerRoll] = prevNum;
                board[currCheckerPos]--;
            }
        }
    }

    private bool isGreaterWhitePointe(int currPos) {
        for(int i = 5; i > currPos; i--) {
            if(board[i] > 0) {
                return true;
            }
        }

        return false;
    }

    private bool isGreaterBlackPointe(int currPos) {
        for (int i = 18; i < currPos; i++) {
            if (board[i] < 0) {
                return true;
            }
        }

        return false;
    }

    private bool whiteCanBearOff() {
        int numWhiteInHome = 0;

        for(int i = 0; i < 6; i++) {
            if(board[i] > 0) {
                numWhiteInHome += board[i];
            }
        }
        numWhiteInHome += board[26];

        if(numWhiteInHome == MAXCHECKERS) {
            return true;
        }

        return false;
    }

    private bool blackCanBearOff() {
        int numBlackInHome = 0;

        for(int i = 18; i < MAXPOINTES; i++) {
            if(board[i] < 0) {
                numBlackInHome += board[i];
            }
        }

        numBlackInHome += -board[27];

        if (-numBlackInHome == MAXCHECKERS) {
            return true;
        }

        return false;
    }

    private int calculateBoardPos(Vector3 pos, bool isChecker) {
        if(isChecker) {
            //pointes 1-6
            if (pos.x > 0 && pos.z > -7) {
                //Pointe 1
                if(pos.z > -1) {
                    return Mathf.Abs(Mathf.RoundToInt(pos.z));
                }

                //Pointes 2-6
                return Mathf.Abs(Mathf.RoundToInt(pos.z)) - 1;
            }
            //pointes 7-12
            else if (pos.x > 0 && pos.z < -7) {
                //pointes 7-9
                if (pos.z > -11) {
                    return Mathf.Abs(Mathf.RoundToInt(pos.z)) - 2;
                }

                //pointes 10-12
                return Mathf.Abs(Mathf.RoundToInt(pos.z)) - 3;
            }
            //pointes 13-18
            else if (pos.x < 0 && pos.z < -7) {
                //pointes 13-15
                if (pos.z < -11) {
                    return MAXPOINTES - Mathf.Abs(Mathf.RoundToInt(pos.z)) + 2;
                }

                //pointes 16-18
                return MAXPOINTES - Mathf.Abs(Mathf.RoundToInt(pos.z)) + 1;
            }
            //pointes 19-24
            else {
                //Pointes 19-23
                if(pos.z < -1) {
                    return MAXPOINTES - Mathf.Abs(Mathf.RoundToInt(pos.z));
                }

                //Pointe 24
                return MAXPOINTES - Mathf.Abs(Mathf.RoundToInt(pos.z)) - 1;
            }
        }
        //Dealing with pointe pos
        else {
            //pointes 1-6
            if (pos.x > 0 && pos.z > -7) {
                //pointes 1-5
                if(pos.z > -5) {
                    return Mathf.Abs(Mathf.RoundToInt(pos.z));
                }

                //pointe 6
                return Mathf.Abs(Mathf.RoundToInt(pos.z)) - 1;
            }
            //pointes 7-12
            else if (pos.x > 0 && pos.z < -7) {
                return Mathf.Abs(Mathf.RoundToInt(pos.z)) - 2;
            }
            //pointes 13-18
            else if (pos.x < 0 && pos.z < -7) {
                return MAXPOINTES - Mathf.Abs(Mathf.RoundToInt(pos.z)) + 1;
            }
            //pointes 19-24
            else {
                //pointe 19
                if (pos.z < -5) {
                    return MAXPOINTES - Mathf.Abs(Mathf.RoundToInt(pos.z));
                }

                //pointes 20-24
                return MAXPOINTES - Mathf.Abs(Mathf.RoundToInt(pos.z)) - 1;
            }
        }
    }

    IEnumerator rollDice() {
        rolledValues = dice.throwDice(true);

        yield return new WaitForSeconds(waitForDiceRollTime);

        movesLeftForTurn = 2;
        usedRoll1 = false;
        usedRoll2 = false;

        if (rolledValues[0] == rolledValues[1]) {
            movesLeftForTurn = 4;
        }
        
        canUseBothMoves = false;
        
        bool canUseMove1 = highlightCheckersMoveable(rolledValues[0]);
        bool canUseMove2 = highlightCheckersMoveable(rolledValues[1]);

        if(canUseMove1 && canUseMove2) {
            canUseBothMoves = true;
        }
        else if(canUseMove1 && !canUseMove2){
            refineHighlightedCheckers(rolledValues[0], rolledValues[1]);
        }
        else if (!canUseMove1 && canUseMove2) {
            refineHighlightedCheckers(rolledValues[1], rolledValues[0]);
        }
        else {
            //No move possible, switch turns
            dice.resetDicePos();

            if (currGamePhase == turnPhase.WhiteTurn) {
                currGamePhase = turnPhase.BlackTurn;
            }
            else {
                currGamePhase = turnPhase.WhiteTurn;
            }
        }
    }

}
*/