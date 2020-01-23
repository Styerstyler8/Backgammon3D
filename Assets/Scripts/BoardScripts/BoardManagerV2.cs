using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//This controls most of the game and maintains board states and communicates with the rest of the pieces to ensure the game progresses correctly
public class BoardManagerV2 : MonoBehaviour {
    private const int MAINSCREENBUILDINDEX = 0;
    //Player is white checkers
    private const int MAXPOINTES = 24;
    private const int MAXCHECKERS = 15;

    //Winning text information
    private const string WHITEWON = "White wins!";
    private const string BLACKWON = "Black wins!";

    //AI table information
    private const string AITABLEBEGIN = "\nAI Move Choices:\nMove   |    Win%\n----------------------\n";

    //Needs references to all pointes, checkers, dice, the bar, barring off location, and the AI used
    [SerializeField] PointeManager[] pointes = new PointeManager[MAXPOINTES];
    [SerializeField] Checker[] whiteCheckers = new Checker[MAXCHECKERS];
    [SerializeField] Checker[] blackCheckers = new Checker[MAXCHECKERS];
    [SerializeField] DiceManager dice = null;
    [SerializeField] BarMananger bar = null;
    [SerializeField] BaringOffManager baringOff = null;
    [SerializeField] MonteCarloAI AI = null;

    //Reference to the winning text information and the AI table win %
    [SerializeField] Button winInformation;
    [SerializeField] Text winInformationText;
    [SerializeField] Button AIMoveInformation;
    [SerializeField] Text AIMoveInformationText;

    //Configurable time to allow dice to roll and have time in between turns
    [SerializeField] float waitForDiceRollTime = 2f;
    [SerializeField] float waitFinishMove = 2f;

    //Configurable distance to lift a checker above the board when selected
    [SerializeField] float selectedPieceYOffset = 2f;

    //Indicates if player v player or player v AI
    public static bool againstAI = false;

    //Keeps track of the current player's turn and what actions they are allowed to do
    private enum turnPhase { StartGame, RollDice, WhiteTurn, BlackTurn };
    private enum gamePhase { Regular, BaringOff };

    private turnPhase currGamePhase;
    private gamePhase whiteCheckerPhase;
    private gamePhase blackCheckerPhase;

    //Indicates how many moves the current player has (2 to start (if possible), 4 for doubles)
    private int movesLeftForTurn = -1;

    //Will let us know which roll has been used when in the middle of a turn
    private bool usedRoll1 = false;
    private bool usedRoll2 = false;
    private bool canUseBothMoves = true;

    //Will store the current turn's rolled values
    private int[] rolledValues = new int[2];
   

    //Will hold information about a selected checker
    private Vector3 selectedCheckerOrigPos = new Vector3(0f, 0f, 0f);
    private Checker selectedChecker = null;
    private int selectedCheckerOrigBoardPos = -1;

    //Will hold the pointes, bar information, and barring off information
    //Ex. board[0] = 1 means 1 white checker on pointe 0, board[17] = -5 means 5 black checkers on pointe 17
    private int[] board = new int[MAXPOINTES + 4];

    //Will contain a list of legal moves a player can make, where the key is a checker's original location and the value is a possible move location
    private List<KeyValuePair<int, int>> legalMoves = new List<KeyValuePair<int, int>>();

    /* board[23] is last pointe
     * board[24-25] is whiteBar and blackBar respectively
     * board[26-27] is white checkers bared off and black checkers bared off
     */

        //On scene start, set up the board
    private void Start() {
        dice.resetDicePos(); //Sets dice to rest position
        arrangeBoard(); //Arrange starting board configuration
        assignPointePos(); //Ensure each pointe knows which pointe it is

        //Display AI table if against AI
        if(againstAI) {
            AIMoveInformation.gameObject.SetActive(true);
        }

        //initial game configuration states
        currGamePhase = turnPhase.StartGame;
        whiteCheckerPhase = gamePhase.Regular;
        blackCheckerPhase = gamePhase.Regular;

        //Player order is determined by the first roll, where higher number goes first
        StartCoroutine(determinePlayerOrder());
    }

    IEnumerator determinePlayerOrder() {
        rolledValues = dice.throwDice(false); //This will get rolled values that are not the same, so player order can be determined

        yield return new WaitForSeconds(waitForDiceRollTime); //Yields actions to allow dice to finish rolling before continuing

        //The first dice is for the white checkers player, so if they are the bigger number then they go first
        if (rolledValues[0] > rolledValues[1]) {
            currGamePhase = turnPhase.WhiteTurn;

            movesLeftForTurn = 2;
            usedRoll1 = false;
            usedRoll2 = false;
            getLegalMoves(rolledValues[0], rolledValues[1]); //Get legal moves will highlight each checker that the current player can move
        }
        //Else black checkers player goes first
        else {
            currGamePhase = turnPhase.BlackTurn;
            if(againstAI) {
                StartCoroutine(doAIMove());
                currGamePhase = turnPhase.WhiteTurn; //After AI, give control back to white player
            }
            else {
                movesLeftForTurn = 2;
                usedRoll1 = false;
                usedRoll2 = false;
                getLegalMoves(rolledValues[0], rolledValues[1]);
            }
        }   
    }

    //This will store in a list all possible moves given two rolls and the current game state
    private void getLegalMoves(int roll1, int roll2) {
        //Rolls 1 and 2
        List<KeyValuePair<int, int>> movesRoll1 = new List<KeyValuePair<int, int>>();
        List<KeyValuePair<int, int>> movesRoll2 = new List<KeyValuePair<int, int>>();
        legalMoves.Clear();

        if(currGamePhase == turnPhase.WhiteTurn) {
            if(whiteCheckerPhase == gamePhase.BaringOff) {
                if(!usedRoll1) {
                    movesRoll1 = getBarringOffMoves(roll1, roll2, true);
                }
                if(!usedRoll2) {
                    movesRoll2 = getBarringOffMoves(roll2, roll1, true);
                }
            }
            else {
                if(!usedRoll1) {
                    movesRoll1 = getRegularMoves(roll1, roll2, true);
                }
                if(!usedRoll2) {
                    movesRoll2 = getRegularMoves(roll2, roll1, true);
                }
                
                //In certain situations, there are edge cases to consider for legal moves. Only such edge case occurs when we haven't used either roll and one of the rolls isn't usable yet
                if(!usedRoll1 && !usedRoll2) {
                    int numRoll1Moves = movesRoll1.Count;
                    int numRoll2Moves = movesRoll2.Count;

                    if (numRoll1Moves == 0 && numRoll2Moves > 1) {
                        movesRoll2 = refineRegularMoves(movesRoll2, roll1, true);
                    }
                    else if (numRoll2Moves == 0 && numRoll1Moves > 1) {
                        movesRoll1 = refineRegularMoves(movesRoll1, roll2, true);
                    }
                }
            }
        }
        else {
            if(blackCheckerPhase == gamePhase.BaringOff) {
                if(!usedRoll1) {
                    movesRoll1 = getBarringOffMoves(roll1, roll2, false);
                }
                if(!usedRoll2) {
                    movesRoll2 = getBarringOffMoves(roll2, roll1, false);
                }
            }
            else {
                if(!usedRoll1) {
                    movesRoll1 = getRegularMoves(roll1, roll2, false);
                }
                if(!usedRoll2) {
                    movesRoll2 = getRegularMoves(roll2, roll1, false);
                }
        
                if(!usedRoll1 && !usedRoll2) {
                    int numRoll1Moves = movesRoll1.Count;
                    int numRoll2Moves = movesRoll2.Count;

                    if (numRoll1Moves == 0 && numRoll2Moves > 1) {
                        movesRoll2 = refineRegularMoves(movesRoll2, roll1, false);
                    }
                    else if (numRoll2Moves == 0 && numRoll1Moves > 1) {
                        movesRoll1 = refineRegularMoves(movesRoll1, roll2, false);
                    }
                }
            }
        }

        //Once we have gotten each possible move for both rolls, add it to legalmoves so it can be highlighted
        foreach(var move in movesRoll1) {
            legalMoves.Add(move);
        }

        foreach(var move in movesRoll2) {
            legalMoves.Add(move);
        }

        //If no legal moves, turn over and update game state
        if(legalMoves.Count == 0) {
            //Switch sides
            dice.resetDicePos();

            if (currGamePhase == turnPhase.WhiteTurn) {
                currGamePhase = turnPhase.BlackTurn;
                if(againstAI) {
                    rolledValues = dice.throwDice(true);
                    StartCoroutine(doAIMove());
                    currGamePhase = turnPhase.WhiteTurn;
                }
            }
            else {
                currGamePhase = turnPhase.WhiteTurn;
            }
        }
        else {
            //Highlight moveable checkers
            highlightCheckersMoveable();
        }
    }

    //This goes through each possible move a player can do given a roll and will highlight each checker that is moveable
    private void highlightCheckersMoveable() {
        foreach (var move in legalMoves) {
            if (move.Key >= MAXPOINTES) {
                if (currGamePhase == turnPhase.WhiteTurn) {
                    bar.highlightChecker(true);
                }
                else {
                    bar.highlightChecker(false);
                }
            }
            else {
                pointes[move.Key].highlightLastChecker(true);
            }
        }
    }

    //Returns list of possible moves given a roll, which color to consider, and when we are not barring off
    private List<KeyValuePair<int, int>> getRegularMoves(int roll, int nonUsedRoll, bool isWhite) {
        List<KeyValuePair<int, int>> moves = new List<KeyValuePair<int, int>>();

        if(isWhite) {
            //Check if checker on bar
            if (board[24] > 0) {
                //Checker(s) on bar, must have rules here
                if (board[MAXPOINTES - roll] > -2) {
                    //Ensure that larger number isnt only possible move
                    if (board[24] == 1 && nonUsedRoll > roll && board[MAXPOINTES - nonUsedRoll] > -2 && !anyValidMoves(roll, nonUsedRoll, 24, true, true)) {
                        //Cant do smaller move then
                    }
                    else {
                        moves.Add(new KeyValuePair<int, int>(24, MAXPOINTES - roll));
                    }
                }
            }
            else {
                //Normal play
                for (int i = MAXPOINTES; i >= roll; i--) {
                    if (board[i] > 0 && board[i - roll] > -2) {
                        if (nonUsedRoll > roll && (i - nonUsedRoll) >= 0 && board[i - nonUsedRoll] > -2 && !anyValidMoves(roll, nonUsedRoll, i, false, true)) {
                            //Cant do smaller move then
                        }
                        else {
                            moves.Add(new KeyValuePair<int, int>(i, i - roll));
                        }
                    }
                }
            }
        }
        else {
            //Check if checker on bar
            if (board[25] < 0) {
                //Checker(s) on bar, must have rules here
                if (board[roll - 1] < 2) {
                    //Ensure that larger number isnt only possible move
                    if (board[25] == -1 && nonUsedRoll > roll && board[nonUsedRoll - 1] < 2 && !anyValidMoves(roll, nonUsedRoll, 25, true, false)) {
                        //Cant do smaller move then
                    }
                    else {
                        moves.Add(new KeyValuePair<int, int>(25, roll - 1));
                    }
                }
            }
            else {
                //Normal play
                for (int i = (MAXPOINTES - roll - 1); i >= 0; i--) {
                    if (board[i] < 0 && board[i + roll] < 2) {
                        if (nonUsedRoll > roll && (i + nonUsedRoll) < MAXPOINTES && board[i + nonUsedRoll] < 2 && !anyValidMoves(roll, nonUsedRoll, i, false, false)) {
                            //Cant do smaller move then
                        }
                        else {
                            moves.Add(new KeyValuePair<int, int>(i, i + roll));
                        }
                    }
                }
            }
        }

        return moves;
    }

    //Deals with a certain edge case described below
    private List<KeyValuePair<int, int>> refineRegularMoves(List<KeyValuePair<int, int>> legalMoves, int unusedRoll, bool isWhite) {
        /*i i o i i o | i i i i x i

          i x i i i i | o i i i i i
          roll 3,5 case where double move required
          
          Checking for above case
        */

        List<KeyValuePair<int, int>> refinedMoves = new List<KeyValuePair<int, int>>();

        if(isWhite) {
            foreach (var move in legalMoves) {
                int pointeToConsider = move.Value - unusedRoll;
                if (pointeToConsider >= 0 && board[move.Value - unusedRoll] > -2) {
                    //We must only do this kind of move, can now use both rolls
                    refinedMoves.Add(move);
                }
            }
        }
        else {
            foreach (var move in legalMoves) {
                int pointeToConsider = move.Value + unusedRoll;
                if (pointeToConsider < MAXPOINTES && board[move.Value + unusedRoll] < 2) {
                    //We must only do this kind of move, can now use both rolls
                    refinedMoves.Add(move);
                }
            }
        }

        if (refinedMoves.Count > 0) {
            return refinedMoves;
        }

        return legalMoves;
    }

    //Returns if there are any possible moves given the rolls and the position of a checker
    //This is a hypothetical check and doesn't impact the board, used for getting possible moves and considering edge cases
    private bool anyValidMoves(int smallerRoll, int largerRoll, int origPos, bool barCase, bool isWhite) {
        bool foundLegalMove = false;

        if(isWhite) {
            board[origPos]--;

            if (barCase) {
                board[MAXPOINTES - smallerRoll]++;
            }
            else {
                board[origPos - smallerRoll]++;
            }

            if(whiteCanBearOff()) {
                foundLegalMove = true;
            }
            else {
                for (int i = MAXPOINTES; i >= largerRoll; i--) {
                    if (board[i] > 0 && board[i - largerRoll] > -2) {
                        foundLegalMove = true;
                        break;
                    }
                }
            }

            board[origPos]++;
            if (barCase) {
                board[MAXPOINTES - smallerRoll]--;
            }
            else {
                board[origPos - smallerRoll]--;
            }
        }
        else {
            board[origPos]++;

            if (barCase) {
                board[smallerRoll - 1]--;
            }
            else {
                board[origPos + smallerRoll]--;
            }

            if(blackCanBearOff()) {
                foundLegalMove = true;
            }
            else {
                for (int i = 0; i < MAXPOINTES - largerRoll; i++) {
                    if (board[i] < 0 && board[i + largerRoll] < 2) {
                        foundLegalMove = true;
                        break;
                    }
                }
            }

            board[origPos]--;
            if (barCase) {
                board[smallerRoll - 1]++;
            }
            else {
                board[origPos + smallerRoll]++;
            }
        }

        return foundLegalMove;
    }

    //Returns all possible moves when in the phase of barring off
    private List<KeyValuePair<int, int>> getBarringOffMoves(int roll, int nonUsedRoll, bool isWhite) {
        List<KeyValuePair<int, int>> moves = new List<KeyValuePair<int, int>>();

        if(isWhite) {
            if (board[roll - 1] > 0) {
                //Checker needs to be moved
                moves.Add(new KeyValuePair<int, int>(roll - 1, 26));
            }
            else {
                bool foundValidChecker = false;
                //Check for higher num checker
                for (int i = 5; i > (roll - 1); i--) {
                    if (board[i] > 0 && board[i - roll] > -2) {

                        if (nonUsedRoll > 0 && (i - nonUsedRoll) == -1) {
                            //Do not add move, has to use other roll
                        }
                        else {
                            //Add to moves
                            moves.Add(new KeyValuePair<int, int>(i, i - roll));
                            foundValidChecker = true;
                        }
                    }
                }

                if (!foundValidChecker) {
                    for (int i = roll - 2; i >= 0; i--) {
                        if (board[i] > 0) {

                            if (nonUsedRoll > 0 && (i - nonUsedRoll) == -1) {
                                //Do not add move, has to use other roll
                            }
                            else {
                                //Return only this move
                                moves.Add(new KeyValuePair<int, int>(i, 26));
                                break;
                            }
                        }
                    }
                }
            }
        }
        else {
            if (board[MAXPOINTES - roll] < 0) {
                //Checker needs to be moved
                moves.Add(new KeyValuePair<int, int>(MAXPOINTES - roll, 27));
            }
            else {
                bool foundValidChecker = false;
                //Check for higher num checker
                for (int i = 18; i < (MAXPOINTES - roll); i++) {
                    if (board[i] < 0 && board[i + roll] < 2) {

                        if (nonUsedRoll > 0 && (i + nonUsedRoll) == MAXPOINTES) {
                            //Do not add move, has to use other roll
                        }
                        else {
                            //Add to moves
                            moves.Add(new KeyValuePair<int, int>(i, i + roll));
                            foundValidChecker = true;
                        }
                    }
                }

                if (!foundValidChecker) {
                    for (int i = MAXPOINTES - roll + 1; i < MAXPOINTES; i++) {
                        if (board[i] < 0) {

                            if (nonUsedRoll > 0 && (i + nonUsedRoll) == MAXPOINTES) {
                                //Do not add move, has to use other roll
                            }
                            else {
                                //Return only this move
                                moves.Add(new KeyValuePair<int, int>(i, 27));
                                break;
                            }
                        }
                    }
                }
            }
        }

        return moves;
    }

    //After a move is done, the checkers must be reset to a normal, nonclickable state, so this is called
    private void removeCheckerHighlights() {
        for(int i = 0; i < MAXCHECKERS; i++) {
            whiteCheckers[i].changeHighlightChecker(false);
            blackCheckers[i].changeHighlightChecker(false);
        }
    }

    //Goes through and manually creates the initial board state of backgammon
    private void arrangeBoard() {
        pointes[0].addChecker(blackCheckers[0]);
        pointes[0].addChecker(blackCheckers[1]);
        board[0] = -2;
        blackCheckers[0].setPos(0);
        blackCheckers[1].setPos(0);

        pointes[5].addChecker(whiteCheckers[0]);
        pointes[5].addChecker(whiteCheckers[1]);
        pointes[5].addChecker(whiteCheckers[2]);
        pointes[5].addChecker(whiteCheckers[3]);
        pointes[5].addChecker(whiteCheckers[4]);
        board[5] = 5;
        whiteCheckers[0].setPos(5);
        whiteCheckers[1].setPos(5);
        whiteCheckers[2].setPos(5);
        whiteCheckers[3].setPos(5);
        whiteCheckers[4].setPos(5);

        pointes[7].addChecker(whiteCheckers[5]);
        pointes[7].addChecker(whiteCheckers[6]);
        pointes[7].addChecker(whiteCheckers[7]);
        board[7] = 3;
        whiteCheckers[5].setPos(7);
        whiteCheckers[6].setPos(7);
        whiteCheckers[7].setPos(7);

        pointes[11].addChecker(blackCheckers[2]);
        pointes[11].addChecker(blackCheckers[3]);
        pointes[11].addChecker(blackCheckers[4]);
        pointes[11].addChecker(blackCheckers[5]);
        pointes[11].addChecker(blackCheckers[6]);
        board[11] = -5;
        blackCheckers[2].setPos(11);
        blackCheckers[3].setPos(11);
        blackCheckers[4].setPos(11);
        blackCheckers[5].setPos(11);
        blackCheckers[6].setPos(11);

        pointes[12].addChecker(whiteCheckers[8]);
        pointes[12].addChecker(whiteCheckers[9]);
        pointes[12].addChecker(whiteCheckers[10]);
        pointes[12].addChecker(whiteCheckers[11]);
        pointes[12].addChecker(whiteCheckers[12]);
        board[12] = 5;
        whiteCheckers[8].setPos(12);
        whiteCheckers[9].setPos(12);
        whiteCheckers[10].setPos(12);
        whiteCheckers[11].setPos(12);
        whiteCheckers[12].setPos(12);

        pointes[16].addChecker(blackCheckers[7]);
        pointes[16].addChecker(blackCheckers[8]);
        pointes[16].addChecker(blackCheckers[9]);
        board[16] = -3;
        blackCheckers[7].setPos(16);
        blackCheckers[8].setPos(16);
        blackCheckers[9].setPos(16);

        pointes[18].addChecker(blackCheckers[10]);
        pointes[18].addChecker(blackCheckers[11]);
        pointes[18].addChecker(blackCheckers[12]);
        pointes[18].addChecker(blackCheckers[13]);
        pointes[18].addChecker(blackCheckers[14]);
        board[18] = -5;
        blackCheckers[10].setPos(18);
        blackCheckers[11].setPos(18);
        blackCheckers[12].setPos(18);
        blackCheckers[13].setPos(18);
        blackCheckers[14].setPos(18);

        pointes[23].addChecker(whiteCheckers[13]);
        pointes[23].addChecker(whiteCheckers[14]);
        board[23] = 2;
        whiteCheckers[13].setPos(23);
        whiteCheckers[14].setPos(23);
    }

    //Used for testing purposes, when want to test end of game interaction
    private void makeNearBarringOffBoard() {
        pointes[1].addChecker(whiteCheckers[0]);
        board[1] = 1;
        whiteCheckers[0].setPos(1);

        pointes[2].addChecker(whiteCheckers[1]);
        pointes[2].addChecker(whiteCheckers[2]);
        board[2] = 2;
        whiteCheckers[1].setPos(2);
        whiteCheckers[2].setPos(2);

        pointes[3].addChecker(blackCheckers[0]);
        pointes[3].addChecker(blackCheckers[1]);
        board[3] = -2;
        blackCheckers[0].setPos(3);
        blackCheckers[1].setPos(3);

        pointes[4].addChecker(whiteCheckers[3]);
        pointes[4].addChecker(whiteCheckers[4]);
        pointes[4].addChecker(whiteCheckers[5]);
        pointes[4].addChecker(whiteCheckers[6]);
        pointes[4].addChecker(whiteCheckers[7]);
        board[4] = 5;
        whiteCheckers[3].setPos(4);
        whiteCheckers[4].setPos(4);
        whiteCheckers[5].setPos(4);
        whiteCheckers[6].setPos(4);
        whiteCheckers[7].setPos(4);

        pointes[5].addChecker(whiteCheckers[8]);
        pointes[5].addChecker(whiteCheckers[9]);
        pointes[5].addChecker(whiteCheckers[10]);
        pointes[5].addChecker(whiteCheckers[11]);
        pointes[5].addChecker(whiteCheckers[12]);
        pointes[5].addChecker(whiteCheckers[13]);
        board[5] = 6;
        whiteCheckers[8].setPos(5);
        whiteCheckers[9].setPos(5);
        whiteCheckers[10].setPos(5);
        whiteCheckers[11].setPos(5);
        whiteCheckers[12].setPos(5);
        whiteCheckers[13].setPos(5);

        pointes[7].addChecker(whiteCheckers[14]);
        board[7] = 1;
        whiteCheckers[14].setPos(7);

        pointes[18].addChecker(blackCheckers[2]);
        pointes[18].addChecker(blackCheckers[3]);
        pointes[18].addChecker(blackCheckers[4]);
        pointes[18].addChecker(blackCheckers[5]);
        pointes[18].addChecker(blackCheckers[6]);
        board[18] = -5;
        blackCheckers[2].setPos(18);
        blackCheckers[3].setPos(18);
        blackCheckers[4].setPos(18);
        blackCheckers[5].setPos(18);
        blackCheckers[6].setPos(18);

        pointes[19].addChecker(blackCheckers[7]);
        pointes[19].addChecker(blackCheckers[8]);
        board[19] = -2;
        blackCheckers[7].setPos(19);
        blackCheckers[8].setPos(19);

        pointes[20].addChecker(blackCheckers[9]);
        board[20] = -1;
        blackCheckers[9].setPos(20);

        pointes[21].addChecker(blackCheckers[10]);
        board[21] = -1;
        blackCheckers[10].setPos(21);

        pointes[22].addChecker(blackCheckers[11]);
        pointes[22].addChecker(blackCheckers[12]);
        pointes[22].addChecker(blackCheckers[13]);
        board[22] = -3;
        blackCheckers[11].setPos(22);
        blackCheckers[12].setPos(22);
        blackCheckers[13].setPos(22);

        pointes[23].addChecker(blackCheckers[14]);
        board[23] = -1;
        blackCheckers[14].setPos(23);
    }

    //Allows each pointe to know what pointe it is
    private void assignPointePos() {
        for(int i = 0; i < pointes.Length; i++) {
            pointes[i].setPointePos(i);
        }
    }

    //Update is run every frame and will allow user interaction
    private void Update() {
        //If we have a checker selected and let go of it, then check for possible actions
        if (Input.GetMouseButtonUp(0) && selectedChecker) {
            releaseSelectedChecker();
        }

        //If we have a checker selected, have it update to where the mouse is
        if (selectedChecker) {
            updateSelectedCheckerPos();
        }

        //When left click is clicked, deal with the appropiate action
        if (Input.GetMouseButtonDown(0)) {
            handleClickAction();
        }

        //If right click is pressed while a checker is selected, then the checker will be put back down to the original position
        if (Input.GetMouseButtonDown(1) && selectedChecker) {
            resetSelectedChecker();
        }
    }

    //Deal with releasing a selected checker, either moving it or resetting it
    private void releaseSelectedChecker() {
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

                newPointePos = pointeSelected.getPos();

                //Look at board seperately for white phase vs black
                if (currGamePhase == turnPhase.WhiteTurn) {
                    //Check for hit and do actions if hit
                    if (board[newPointePos] == -1) {
                        blackCheckerPhase = gamePhase.Regular;
                        Checker removedChecker = pointes[newPointePos].removeChecker();
                        removedChecker.setPos(25);
                        bar.addCheckerOnBar(removedChecker);
                        board[newPointePos] = 0;
                        board[25]--;
                    }

                    if (board[24] > 0) {
                        Checker removedChecker = bar.removeCheckerOnBar(true);
                        removedChecker.setPos(newPointePos);
                        pointes[newPointePos].addChecker(removedChecker);
                        selectedCheckerOrigBoardPos = 24;
                        board[24]--;
                    }
                    //Not on bar, then regular move and do actions
                    else {
                        Checker removedChecker = pointes[selectedCheckerOrigBoardPos].removeChecker();
                        removedChecker.setPos(newPointePos);
                        pointes[newPointePos].addChecker(removedChecker);
                        board[selectedCheckerOrigBoardPos]--;
                    }

                    board[newPointePos]++;
                }
                else if (currGamePhase == turnPhase.BlackTurn) {
                    //Check for hit and do actions if hit
                    if (board[newPointePos] == 1) {
                        whiteCheckerPhase = gamePhase.Regular;
                        Checker removedChecker = pointes[newPointePos].removeChecker();
                        removedChecker.setPos(24);
                        bar.addCheckerOnBar(removedChecker);
                        board[newPointePos] = 0;
                        board[24]++;
                    }

                    if (board[25] < 0) {
                        Checker removedChecker = bar.removeCheckerOnBar(false);
                        removedChecker.setPos(newPointePos);
                        pointes[newPointePos].addChecker(removedChecker);
                        selectedCheckerOrigBoardPos = -1;
                        board[25]++;
                    }
                    //Not on bar, then regular move and do actions
                    else {
                        Checker removedChecker = pointes[selectedCheckerOrigBoardPos].removeChecker();
                        removedChecker.setPos(newPointePos);
                        pointes[newPointePos].addChecker(removedChecker);
                        board[selectedCheckerOrigBoardPos]++;
                    }

                    board[newPointePos]--;
                }
            }
            else if (clickedBaringOff && clickedBaringOff.isClickable()) {
                clickedPointe = true;
                Checker removedChecker = pointes[selectedCheckerOrigBoardPos].removeChecker();
                baringOff.addCheckerBaringOff(removedChecker);

                if (currGamePhase == turnPhase.WhiteTurn) {
                    removedChecker.setPos(26);
                    board[selectedCheckerOrigBoardPos]--;
                    board[26]++;
                    newPointePos = -1;
                }
                else {
                    removedChecker.setPos(27);
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

            if (currGamePhase == turnPhase.WhiteTurn) {
                if (board[26] == MAXCHECKERS) {
                    StartCoroutine(finishGame(true));
                }

                if (whiteCanBearOff()) {
                    whiteCheckerPhase = gamePhase.BaringOff;
                }
            }
            else {
                if (-board[27] == MAXCHECKERS) {
                    StartCoroutine(finishGame(false));
                }

                if (blackCanBearOff()) {
                    blackCheckerPhase = gamePhase.BaringOff;
                }
            }

            if (movesLeftForTurn == 0) {
                //Switch turns
                dice.resetDicePos();

                if (currGamePhase == turnPhase.WhiteTurn) {
                    currGamePhase = turnPhase.BlackTurn;
                    if (againstAI) {
                        rolledValues = dice.throwDice(true);
                        StartCoroutine(doAIMove());
                        currGamePhase = turnPhase.WhiteTurn;
                    }
                }
                else {
                    currGamePhase = turnPhase.WhiteTurn;
                }
            }
            else if (rolledValues[0] == rolledValues[1]) {
                usedRoll1 = true;
                getLegalMoves(-1, rolledValues[1]);
            }
            else if (Mathf.Abs(newPointePos - selectedCheckerOrigBoardPos) == rolledValues[0]) {
                usedRoll1 = true;
                getLegalMoves(-1, rolledValues[1]);
            }
            else {
                usedRoll2 = true;
                getLegalMoves(rolledValues[0], -1);
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

    //Update selected checker screen pos
    private void updateSelectedCheckerPos() {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit)) {
            selectedChecker.transform.position = new Vector3(hit.point.x, selectedPieceYOffset, hit.point.z);
        }
    }

    //Called when left click is pressed
    private void handleClickAction() {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit)) {
            Checker clickedChecker = hit.transform.GetComponent<Checker>();
            Dice clickedDice = hit.transform.GetComponent<Dice>();

            //If we clicked on a clickable checker, then select that checker to lift it up and move around, and highlight the pointes it can move to
            if (clickedChecker && clickedChecker.isClickable()) {
                selectedChecker = clickedChecker;
                selectedCheckerOrigPos = selectedChecker.transform.position;

                selectedCheckerOrigBoardPos = selectedChecker.getPos();

                highlightPointes(selectedCheckerOrigBoardPos);
            }
            //If we clicked the dice and its time to roll them, then roll the dice
            else if (clickedDice && clickedDice.isClickable()) {
                //Conduct dice phase
                StartCoroutine(rollDice());
            }
            //Else, clicked invalid object, don't deal with it
            else {
                //Debug.Log("Clicked invalid object");
            }
        }
    }

    //Puts the selceted checker back to its original position and makes each pointe unclickable, since checker has been released
    private void resetSelectedChecker() {
        selectedChecker.transform.position = selectedCheckerOrigPos;
        selectedChecker = null;
        selectedCheckerOrigPos = Vector3.zero;
        selectedCheckerOrigBoardPos = -1;
        resetPointes();
    }

    //After a checker is placed down, the pointes are returned to normal and made nonclickable
    private void resetPointes() {
        baringOff.changeHighlightBaringOff(false);
        for (int i = 0; i < pointes.Length; i++) {
            pointes[i].changeHighlightPointe(false);
        }
    }

    //Highlights each pointe a given checker can move to
    private void highlightPointes(int boardPos) {
        foreach(var move in legalMoves) {
            if(move.Key == boardPos) {
                if(move.Value > MAXPOINTES) {
                    baringOff.changeHighlightBaringOff(true);
                }
                else {
                    pointes[move.Value].changeHighlightPointe(true);
                }
            }
        }
    }

    //Used to check if white checkers can enter the end game
    private bool whiteCanBearOff() {
        int numWhiteInHome = 0;

        for (int i = 0; i < 6; i++) {
            if (board[i] > 0) {
                numWhiteInHome += board[i];
            }
        }
        numWhiteInHome += board[26];

        if (numWhiteInHome == MAXCHECKERS) {
            return true;
        }

        return false;
    }

    //Used to check if black checkers can enter the end game
    private bool blackCanBearOff() {
        int numBlackInHome = 0;

        for (int i = 18; i < MAXPOINTES; i++) {
            if (board[i] < 0) {
                numBlackInHome += board[i];
            }
        }

        numBlackInHome += board[27];

        if (-numBlackInHome == MAXCHECKERS) {
            return true;
        }

        return false;
    }

    //Rolls the dice and calls for each possible checker that can be moved to be highlighted
    IEnumerator rollDice() {
        rolledValues = dice.throwDice(true);

        yield return new WaitForSeconds(waitForDiceRollTime);

        if(currGamePhase == turnPhase.BlackTurn && againstAI) {
            StartCoroutine(doAIMove());
            currGamePhase = turnPhase.WhiteTurn;
        }
        else {
            movesLeftForTurn = 2;
            usedRoll1 = false;
            usedRoll2 = false;

            if (rolledValues[0] == rolledValues[1]) {
                movesLeftForTurn = 4;
            }

            getLegalMoves(rolledValues[0], rolledValues[1]);
        } 
    }

    //Calls upon the AI to get the best possible move, given the board state
    IEnumerator doAIMove() {

        yield return new WaitForSeconds(waitFinishMove);

        int roll1 = rolledValues[0];
        int roll2 = rolledValues[1];
        Dictionary<KeyValuePair<int, int>, float> winPercentages = new Dictionary<KeyValuePair<int, int>, float>();

        if(roll1 == roll2) {
            for(int i = 0; i < 4; i++) {
                KeyValuePair<int, int> move = AI.getPlay(board, roll1, roll2, winPercentages);
                displayWinPercentages(winPercentages);

                if(move.Key == -1 && move.Value == -1) {
                    break;
                }

                updateBoard(move);

                yield return new WaitForSeconds(waitFinishMove);
            }
        }
        else {
            KeyValuePair<int, int> move = AI.getPlay(board, roll1, roll2, winPercentages);
            displayWinPercentages(winPercentages);

            if (move.Key != -1 && move.Value != -1) {
                updateBoard(move);
                yield return new WaitForSeconds(waitFinishMove);

                if (Mathf.Abs(move.Key - move.Value) == roll1) {
                    move = AI.getPlay(board, roll2, -1, winPercentages);
                    displayWinPercentages(winPercentages);

                    if (move.Key != -1 && move.Value != -1) {
                        updateBoard(move);
                    }
                }
                else {
                    move = AI.getPlay(board, roll1, -1, winPercentages);
                    displayWinPercentages(winPercentages);

                    if (move.Key != -1 && move.Value != -1) {
                        updateBoard(move);
                    }
                }
            }
        }

        if(-board[27] == MAXCHECKERS) {
            StartCoroutine(finishGame(false));
        }

        if(blackCanBearOff()) {
            blackCheckerPhase = gamePhase.BaringOff;
        }

        dice.resetDicePos();
    }

    //Updates the table figure to show win percentages for last AI move possibilities
    private void displayWinPercentages(Dictionary<KeyValuePair<int, int>, float> winPercentages) {
        string AITableText = AITABLEBEGIN;
        foreach(var move in winPercentages) {
            string nextRow = "<" + move.Key.Key + "," + move.Key.Value + ">  ->  " + move.Value.ToString("0.00") + "%\n";
            AITableText += nextRow;
        }

        AIMoveInformationText.text = AITableText;
        winPercentages.Clear();
    }

    //Update board after AI move, given the move the AI selected, AI = black checkers
    private void updateBoard(KeyValuePair<int, int> move) {
        //Check for hit and do actions if hit
        if (board[move.Value] == 1) {
            whiteCheckerPhase = gamePhase.Regular;
            Checker removedChecker = pointes[move.Value].removeChecker();
            removedChecker.setPos(24);
            bar.addCheckerOnBar(removedChecker);
            board[move.Value] = 0;
            board[24]++;
        }

        //Happens when checkers exist on bar, then must deal with them
        if (board[25] < 0) {
            Checker removedChecker = bar.removeCheckerOnBar(false);
            removedChecker.setPos(move.Value);
            pointes[move.Value].addChecker(removedChecker);
            board[25]++;
        }
        //Not on bar, then check barring off then check regular move and do actions
        else if(move.Value > MAXPOINTES) {
            Checker removedChecker = pointes[move.Key].removeChecker();
            baringOff.addCheckerBaringOff(removedChecker);
            removedChecker.setPos(27);
            board[move.Key]++;
            board[27]--;
        }
        else {
            Checker removedChecker = pointes[move.Key].removeChecker();
            removedChecker.setPos(move.Value);
            pointes[move.Value].addChecker(removedChecker);
            board[move.Key]++;
        }

        board[move.Value]--;
    }

    IEnumerator finishGame(bool whiteHasWon) {
        AIMoveInformation.gameObject.SetActive(false);
        winInformation.gameObject.SetActive(true);
        if (whiteHasWon) {
            winInformationText.text = WHITEWON;
        }
        else {
            winInformationText.text = BLACKWON;
        }

        yield return new WaitForSeconds(waitFinishMove);

        SceneManager.LoadScene(MAINSCREENBUILDINDEX); //Go back to title screen
    }
}