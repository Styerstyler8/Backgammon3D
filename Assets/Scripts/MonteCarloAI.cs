using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The AI of the game, based off of the heuristic search algorithm Monte Carlo Tree Search
public class MonteCarloAI : MonoBehaviour {
    //Equality comparer needed to distinguish differences between board states
    public class CustomEqualityComparer : IEqualityComparer<int[]> {
        public bool Equals(int[] x, int[] y) {
            if (x.Length != y.Length) {
                return false;
            }
            for (int i = 0; i < x.Length; i++) {
                if (x[i] != y[i]) {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(int[] obj) {
            int result = 17;
            for (int i = 0; i < obj.Length; i++) {
                unchecked {
                    result = result * 23 + obj[i];
                }
            }
            return result;
        }
    }

    //Board is a custom class for defining the state of a game, which is determined by the int array representing the board and which turn it is
    private class Board {
        private int[] board = new int[MAXPOINTES + 4];
        private bool whiteTurn = false;

        public Board() {
            board = new int[MAXPOINTES + 4];
            whiteTurn = false;
        }

        public Board(int[] origBoard, bool isWhiteTurn) {
            board = new int[MAXPOINTES + 4];
            for (int i = 0; i < origBoard.Length; i++) {
                board[i] = origBoard[i];
            }

            whiteTurn = isWhiteTurn;
        }

        public int[] getBoard() {
            return board;
        }

        public void setBoard(int[] origBoard) {
            for (int i = 0; i < origBoard.Length; i++) {
                board[i] = origBoard[i];
            }
        }

        public bool isWhiteTurn() {
            return whiteTurn;
        }

        public void setTurn(bool nowWhite) {
            whiteTurn = nowWhite;
        }

    }

    private const int MAXPOINTES = 24;
    private const int MAXCHECKERS = 15;
    private int minDiceRollNum = 1;
    private int maxDiceRollNum = 6;
    private int maxDepth = 0;
    private bool usedMove1 = false;
    private bool usedMove2 = false;

    private List<Board> states = new List<Board>();
    private Dictionary<int[], int> wins = new Dictionary<int[], int>(new CustomEqualityComparer()); //Wins is a dictionary mapping board states to number of wins from that board state
    private Dictionary<int[], int> plays = new Dictionary<int[], int>(new CustomEqualityComparer()); //Plays is dictionary mapping board states to number of plays from that board state

    public static float allocatedTime = 30f; //How much time a move is allowed to take up
    [SerializeField] int maxMoves = 100; //Max moves allowed in one turn
    [SerializeField] float explorationParam = 1.4f; //Used in equation to determine which child to visit
    
    //Get play takes in a board and the rolls for the AI, returns the best move given this information
    public KeyValuePair<int, int> getPlay(int[] origBoard, int roll1, int roll2, Dictionary<KeyValuePair<int, int>, float> winPercentages) {
        maxDepth = 0;
        int[] b = new int[origBoard.Length];
        
        //Want deep copy of the board, do not want to overwrite board or will mess up main game. It is passed by reference
        for(int i = 0; i < origBoard.Length; i++) {
            b[i] = origBoard[i];
        }

        Board currState = new Board(b, true);
        states.Add(currState);

        List<KeyValuePair<int, int>> legalMoves = getLegalMoves(b, roll1, roll2, false);

        //If no legal moves, return error code which will be dealt with by board manager, or if only 1 move return that move
        if(legalMoves.Count == 0) {
            return new KeyValuePair<int, int>(-1, -1);
        }
        else if(legalMoves.Count == 1) {
            return legalMoves[0];
        }

        int numGames = 0;

        float currTimeRunning = 0f;

        //Keep running simulations for the amount of time allowed
        while (currTimeRunning < allocatedTime) {
            //Run simmulation
            runSimulation(roll1, roll2);
            currTimeRunning += Time.deltaTime;
            numGames += 1;
        }

        Dictionary<KeyValuePair<int, int>, Board> moveStates = new Dictionary<KeyValuePair<int, int>, Board>(); //moveStates will contain move and board result
        foreach (var move in legalMoves) {
            if (!moveStates.ContainsKey(move)) {
                Board boardAfterMove = new Board(updateBoard(b, move, false), true);
                moveStates.Add(move, boardAfterMove);
            }
        }

        float bestWinPercentage = 0f;
        KeyValuePair<int, int> bestMove = legalMoves[0];

        //For each move, calculate best win percentage
        foreach(var move in moveStates) {
            int numWins = 0;
            int playedGames = 0;
            playedGames = plays[move.Value.getBoard()];

            if(wins.ContainsKey(move.Value.getBoard())) {
                numWins = wins[move.Value.getBoard()];
            }

            if(playedGames > 0) {
                float winPercentage = ((float) numWins) / playedGames;
                winPercentages.Add(move.Key, winPercentage);

                if(bestWinPercentage < winPercentage) {
                    bestWinPercentage = winPercentage;
                    bestMove = move.Key;
                }
            }
        }

        return bestMove;
    }

    //Run simulation will go all the way until a winner is found and using random moves at times when necessary, given first rolls
    private void runSimulation(int r1, int r2) {
        HashSet<Board> visitedStates = new HashSet<Board>();

        Board currState = new Board(states[states.Count - 1].getBoard(), states[states.Count - 1].isWhiteTurn());
        
        //Curr player update here
        bool isWhiteTurn = !currState.isWhiteTurn();
        usedMove1 = false;
        usedMove2 = false;

        bool expand = true;

        //Keep going for a certain amount of moves, but will most likely reach winner
        for(int i = 1; i < maxMoves + 1; i++) {
            Board updatedState = new Board();
            int roll1;
            int roll2;

            //Set for the first roll to be the given roll on the first run, or else use random rolls
            if (r1 > 0 && r2 > 0) {
                roll1 = r1;
                roll2 = r2;
            }
            else if(r1 > 0 && r2 < 0) {
                usedMove2 = true;
                roll1 = r1;
                roll2 = -1;
            }
            else if(r1 < 0 && r2 > 0) {
                usedMove1 = true;
                roll1 = -1;
                roll2 = r2;
            }
            else {
                roll1 = Random.Range(minDiceRollNum, maxDiceRollNum + 1);
                roll2 = Random.Range(minDiceRollNum, maxDiceRollNum + 1);
            }

            if(usedMove1) {
                roll1 = -1;
            }
            if(usedMove2) {
                roll2 = -1;
            }
            //Pick random play from legal plays
            
            //Calculate state after random play + Randomly select one of the moves + New state now (apply moves)

            List<KeyValuePair<int, int>> legalMoves = getLegalMoves(currState.getBoard(), roll1, roll2, isWhiteTurn);

            //If no legal moves, change sides
            if(legalMoves.Count == 0) {
                isWhiteTurn = !isWhiteTurn;
                usedMove1 = false;
                usedMove2 = false;
                continue;
            }

            Dictionary<KeyValuePair<int, int>, Board> moveStates = new Dictionary<KeyValuePair<int, int>, Board>(); //moveStates will contain move and board result
            moveStates.Clear();
            foreach (var move in legalMoves) {
                if(!moveStates.ContainsKey(move)) {
                    Board boardAfterMove = new Board(updateBoard(currState.getBoard(), move, isWhiteTurn), isWhiteTurn);
                    moveStates.Add(move, boardAfterMove);
                }
            }

            bool allStatesInPlays = true;
            foreach(var move in moveStates) {
                if(!plays.ContainsKey(move.Value.getBoard())) {
                    allStatesInPlays = false;
                    break;
                }
            }

            //If we have done every possible move before, consider equation to see which move to pick, otherwise make random choice
            if(allStatesInPlays) {
                //Use expression to see which node to explore
                int sumOfMoveStates = 0;
                foreach(var move in moveStates) {
                    sumOfMoveStates += plays[move.Value.getBoard()];
                }

                float logTotal = Mathf.Log((float) sumOfMoveStates);

                float maxValue = 0;
                KeyValuePair<int, int> bestMove;
                bool usingMove1 = true;
                Board bestState = new Board();
                foreach(var move in moveStates) {
                    //Currvalue is our upper bound expression to determine whether to explore a bad option or choose best option
                    float currValue = ((wins[move.Value.getBoard()] / plays[move.Value.getBoard()]) + explorationParam * Mathf.Sqrt(logTotal / plays[move.Value.getBoard()]));

                    if(currValue > maxValue) {
                        maxValue = currValue;
                        bestMove = move.Key;
                        bestState = move.Value;
                        if(Mathf.Abs(move.Key.Key - move.Key.Value) == roll1) {
                            usingMove1 = true;
                        }
                        else {
                            usingMove1 = false;
                        }
                    }
                }

                //Check which roll we use, roll1 or roll2
                if(usingMove1) {
                    usedMove1 = true;
                }
                else {
                    usedMove2 = true;
                }
                updatedState.setBoard(bestState.getBoard());
                updatedState.setTurn(isWhiteTurn);
            }
            else {
                //Make random choice
                updatedState.setBoard(applyRandomMove(currState.getBoard(), roll1, roll2, isWhiteTurn));
                updatedState.setTurn(isWhiteTurn);
            }

            if(usedMove1) {
                r1 = -1;
            }
            if(usedMove2) {
                r2 = -1;
            }


            //Append new state to states
            currState = updatedState;

            if(expand && !plays.ContainsKey(currState.getBoard())) {
                expand = false;
                plays.Add(currState.getBoard(), 0);
                wins.Add(currState.getBoard(), 0);

                if(i > maxDepth) {
                    maxDepth = i;
                }
            }

            visitedStates.Add(currState);



            //If win, break 
            if (won(currState.getBoard(), isWhiteTurn)) {
                break;
            }

            //Switch sides if both moves used
            if(usedMove1 && usedMove2) {
                isWhiteTurn = !isWhiteTurn;
                usedMove1 = false;
                usedMove2 = false;
            }
        }

        //Update the list of plays and wins model, only update if we got to the state by expanding, not by random simulation
        foreach(Board state in visitedStates) {
            if(!plays.ContainsKey(state.getBoard())) {
                continue;
            }

            plays[state.getBoard()] += 1;
            if(isWhiteTurn == false && isWhiteTurn == state.isWhiteTurn()) {
                wins[state.getBoard()] += 1;
            }
        }
    }

    //Return if someone has won the game
    private bool won(int[] b, bool checkWhite) {

        if(checkWhite) { 
            if(b[26] == MAXCHECKERS) {
                return true;
            }

            return false;
        }
        else {
            if (-b[27] == MAXCHECKERS) {
                return true;
            }

            return false;
        } 
    }

    //Return an updated board after a random move is applied
    private int[] applyRandomMove(int[] b, int roll1, int roll2, bool isWhiteTurn) {
        if (b.Length != (MAXPOINTES + 4)) {
            Debug.Log("Incorrect sized array passed in.");
        }

        //Check which checker we are -- Assume black

        //If regular or barring off
        
        bool barringOff = checkBarringOff(b, isWhiteTurn);

        return getRandomMove(b, roll1, roll2, barringOff, isWhiteTurn);
    }

    //Get all possible moves for a given board state
    private List<KeyValuePair<int, int>> getLegalMoves(int[] b, int roll1, int roll2, bool isWhite) {
        List<KeyValuePair<int, int>> legalMoves1 = new List<KeyValuePair<int, int>>();
        List<KeyValuePair<int, int>> legalMoves2 = new List<KeyValuePair<int, int>>();

        if (checkBarringOff(b, isWhite)) {
            if(roll1 > 0) {
                legalMoves1 = getBarringOffMoves(b, roll1, roll2, isWhite);
            }
            if(roll2 > 0) {
                legalMoves2 = getBarringOffMoves(b, roll2, roll1, isWhite);
            }
        }
        else {
            if(roll1 > 0) {
                legalMoves1 = getRegularMoves(b, roll1, roll2, isWhite);
            }
            if(roll2 > 0) {
                legalMoves2 = getRegularMoves(b, roll2, roll1, isWhite);
            }
                
            if(legalMoves1 != null && legalMoves2 != null && legalMoves1.Count == 0 && legalMoves2.Count > 0) {
                legalMoves2 = refineRegularMoves(b, legalMoves2, roll1, isWhite);
            }
            else if(legalMoves2 != null && legalMoves2 != null && legalMoves2.Count == 0 && legalMoves1.Count > 0) {
                legalMoves1 = refineRegularMoves(b, legalMoves1, roll2, isWhite);
            }
        }

        List<KeyValuePair<int, int>> legalMoves = new List<KeyValuePair<int, int>>();

        if(legalMoves1 != null) {
            foreach (var move in legalMoves1) {
                legalMoves.Add(move);
            }
        }

        if(legalMoves2 != null) {
            foreach (var move in legalMoves2) {
                legalMoves.Add(move);
            }
        }

        return legalMoves;
    }

    //Check if someone is barring off given a board state
    private bool checkBarringOff(int[] b, bool checkWhite) {
        if(checkWhite) {
            int numWhiteInHome = 0;

            for(int i = 0; i < 6; i++) {
                if(b[i] > 0) {
                    numWhiteInHome += b[i];
                }
            }

            numWhiteInHome += b[26];

            if(numWhiteInHome == MAXCHECKERS) {
                return true;
            }

            return false;
        }
        else {
            int numBlackInHome = 0;

            for (int i = 18; i < MAXPOINTES; i++) {
                if (b[i] < 0) {
                    numBlackInHome += b[i];
                }
            }

            numBlackInHome += b[27];

            if (-numBlackInHome == MAXCHECKERS) {
                return true;
            }

            return false;
        } 
    }

    //Get all possible moves, pick a random move, and return the board after that move is done
    private int[] getRandomMove(int[] b, int roll1, int roll2, bool isBarringOff, bool checkWhite) {
        KeyValuePair<int, int> pickedMove = new KeyValuePair<int, int>();

        //Rolls 1 and 2
        List<KeyValuePair<int, int>> movesRoll1;
        List<KeyValuePair<int, int>> movesRoll2;

        int numRoll1Moves;
        int numRoll2Moves;

        if (isBarringOff) {
            movesRoll1 = getBarringOffMoves(b, roll1, roll2, checkWhite);

            movesRoll2 = getBarringOffMoves(b, roll2, roll1, checkWhite);

            if(movesRoll1 == null) {
                numRoll1Moves = 0;
            }
            else {
                numRoll1Moves = movesRoll1.Count;
            }

            if(movesRoll2 == null) {
                numRoll2Moves = 0;
            }
            else {
                numRoll2Moves = movesRoll2.Count;
            }
        }
        else {
            movesRoll1 = getRegularMoves(b, roll1, roll2, checkWhite);

            movesRoll2 = getRegularMoves(b, roll2, roll1, checkWhite);

            if (movesRoll1 == null) {
                numRoll1Moves = 0;
            }
            else {
                numRoll1Moves = movesRoll1.Count;
            }

            if (movesRoll2 == null) {
                numRoll2Moves = 0;
            }
            else {
                numRoll2Moves = movesRoll2.Count;
            }

            if (numRoll1Moves == 0 && numRoll2Moves > 1) {
                movesRoll2 = refineRegularMoves(b, movesRoll2, roll1, checkWhite);
                numRoll2Moves = movesRoll2.Count;
            }
            else if(numRoll2Moves == 0 && numRoll1Moves > 1) {
                movesRoll1 = refineRegularMoves(b, movesRoll1, roll2, checkWhite);
                numRoll1Moves = movesRoll1.Count;
            }
        }

        int randomMove;
        int totalPossibleMoves;

        if (numRoll1Moves == 0 && numRoll2Moves == 0) {
            //No moves possible
            usedMove1 = true;
            usedMove2 = true;
            return b;
        }
        else if(numRoll2Moves == 0) {
            //Only consider move1
            randomMove = Random.Range(0, numRoll1Moves);
            pickedMove = movesRoll1[randomMove];
            usedMove1 = true;
        }
        else if(numRoll1Moves == 0) {
            //Only consider move2
            usedMove2 = true;
            randomMove = Random.Range(0, numRoll2Moves);
            pickedMove = movesRoll2[randomMove];
        }
        else {
            //Consider both rolls
            totalPossibleMoves = numRoll1Moves + numRoll2Moves;

            randomMove = Random.Range(0, totalPossibleMoves);

            if(randomMove < numRoll1Moves) {
                //Random move is from roll1
                usedMove1 = true;
                pickedMove = movesRoll1[randomMove];
            }
            else {
                //Random move is from roll2
                usedMove2 = true;
                pickedMove = movesRoll2[randomMove - numRoll1Moves];
            }
        }

        return updateBoard(b, pickedMove, checkWhite);
    }

    //Apply a move to a given board
    private int[] updateBoard(int[] origBoard, KeyValuePair<int, int> movePair, bool whiteTurn) {
        int[] b = new int[origBoard.Length];

        for (int i = 0; i < origBoard.Length; i++) {
            b[i] = origBoard[i];
        }

        if (whiteTurn) {
            b[movePair.Key]--;
            b[movePair.Value]++;
        }
        else {
            b[movePair.Key]++;
            b[movePair.Value]--;
        }
        
        return b;
    }

    //Get list of all possible moves for a given board state and roll
    private List<KeyValuePair<int, int>> getRegularMoves(int[] b, int roll, int nonUsedRoll, bool getWhiteMoves) {
        if(roll < 0) {
            return null;
        }
        List<KeyValuePair<int, int>> moves = new List<KeyValuePair<int, int>>();
        if(getWhiteMoves) {
            //Check if checker on bar
            if (b[24] > 0) {
                //Checker(s) on bar, must have rules here
                if (b[MAXPOINTES - roll] > -2) {
                    //Ensure that larger number isnt only possible move
                    if (b[24] == 1 && nonUsedRoll > roll && b[MAXPOINTES - nonUsedRoll] > -2 && !anyValidMoves(b, roll, nonUsedRoll, 24, true, true)) {
                        //Cant do smaller move then
                    }
                    else {
                        moves.Add(new KeyValuePair<int, int>(24, MAXPOINTES - roll));
                    }
                }
            }
            else {
                //Normal play
                bool checkEdgeCase = true;
                for (int i = MAXPOINTES; i >= roll; i--) {
                    if (b[i] > 0 && b[i - roll] > -2) {
                        if (nonUsedRoll > roll && (i - nonUsedRoll) >= 0 && b[i - nonUsedRoll] > -2 && checkEdgeCase) {
                            if (!anyValidMoves(b, roll, nonUsedRoll, i, false, true)) {
                                //Cant do smaller move then
                                continue;
                            }
                            else {
                                checkEdgeCase = false;
                                moves.Add(new KeyValuePair<int, int>(i, i - roll));
                            }
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
            if (b[25] < 0) {
                //Checker(s) on bar, must have rules here
                if (b[roll - 1] < 2) {
                    //Ensure that larger number isnt only possible move
                    if (b[25] == -1 && nonUsedRoll > roll && b[nonUsedRoll - 1] < 2 && !anyValidMoves(b, roll, nonUsedRoll, 25, true, false)) {
                        //Cant do smaller move then
                    }
                    else {
                        moves.Add(new KeyValuePair<int, int>(25, roll - 1));
                    }
                }
            }
            else {
                //Normal play
                bool checkEdgeCase = true;
                for (int i = (MAXPOINTES - roll - 1); i >= 0; i--) {
                    if (b[i] < 0 && b[i + roll] < 2) {
                        if (nonUsedRoll > roll && (i + nonUsedRoll) < MAXPOINTES && b[i + nonUsedRoll] < 2 && checkEdgeCase) {
                            if (!anyValidMoves(b, roll, nonUsedRoll, i, false, false)) {
                                //Cant do smaller move then
                                continue;
                            }
                            else {
                                checkEdgeCase = false;
                                moves.Add(new KeyValuePair<int, int>(i, i + roll));
                            }
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

    //Checks edge case for certain conditions
    private List<KeyValuePair<int, int>> refineRegularMoves(int[] b, List<KeyValuePair<int, int>> legalMoves, int unusedRoll, bool refineWhite) {
        /*i i o i i o | i i i i x i

          i x i i i i | o i i i i i
          roll 3,5 case where double move required
          
          Checking for above case
        */
        if(unusedRoll < 0) {
            return legalMoves;
        }

        List<KeyValuePair<int, int>> refinedMoves = new List<KeyValuePair<int, int>>();

        if(refineWhite) {
            foreach (var move in legalMoves) {
                int pointeToConsider = move.Value - unusedRoll;
                if (pointeToConsider >= 0 && b[move.Value - unusedRoll] > -2) {
                    //We must only do this kind of move, can now use both rolls
                    refinedMoves.Add(move);
                }
            }
        }
        else {
            foreach (var move in legalMoves) {
                int pointeToConsider = move.Value + unusedRoll;
                if (pointeToConsider < MAXPOINTES && b[move.Value + unusedRoll] < 2) {
                    //We must only do this kind of move, can now use both rolls
                    refinedMoves.Add(move);
                }
            }
        } 

        if(refinedMoves.Count > 0) {
            return refinedMoves;
        }

        return legalMoves;
    }

    //Returns if there are any possible moves given a board state and rolls
    private bool anyValidMoves(int[] origBoard, int smallerRoll, int largerRoll, int origPos, bool barCase, bool checkWhite) {
        int[] b = new int[origBoard.Length];
        for(int i = 0; i < origBoard.Length; i++) {
            b[i] = origBoard[i];
        }

        if(checkWhite) {
            b[origPos]--;

            if (barCase) {
                b[MAXPOINTES - smallerRoll]++;
            }
            else {
                b[origPos - smallerRoll]++;
            }

            if(checkBarringOff(b, checkWhite)) {
                return true;
            }

            for (int i = MAXPOINTES; i >= largerRoll; i--) {
                if (b[i] > 0 && b[i - largerRoll] > -2) {
                    return true;
                }
            }

            return false;
        }
        else {
            b[origPos]++;

            if (barCase) {
                b[smallerRoll - 1]--;
            }
            else {
                b[origPos + smallerRoll]--;
            }

            if (checkBarringOff(b, checkWhite)) {
                return true;
            }

            for (int i = 0; i < MAXPOINTES - largerRoll; i++) {
                if (b[i] < 0 && b[i + largerRoll] < 2) {
                    return true;
                }
            }

            return false;
        }
    }

    //Get possible moves when in barring off state given a board state
    private List<KeyValuePair<int, int>> getBarringOffMoves(int[] b, int roll, int nonUsedRoll, bool getWhite) {
        List<KeyValuePair<int, int>> moves = new List<KeyValuePair<int, int>>();

        if(roll < 0) {
            return null;
        }

        if(getWhite) {
            if (b[roll - 1] > 0) {
                //Checker needs to be moved
                moves.Add(new KeyValuePair<int, int>(roll - 1, 26));
            }
            else {
                bool foundValidChecker = false;
                //Check for higher num checker
                for (int i = 5; i > (roll - 1); i--) {
                    if (b[i] > 0 && b[i - roll] > -2) {

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
                        if (b[i] > 0) {

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
            if (b[MAXPOINTES - roll] < 0) {
                //Checker needs to be moved
                moves.Add(new KeyValuePair<int, int>(MAXPOINTES - roll, 27));
            }
            else {
                bool foundValidChecker = false;
                //Check for higher num checker
                for (int i = 18; i < (MAXPOINTES - roll); i++) {
                    if (b[i] < 0 && b[i + roll] < 2) {

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
                        if (b[i] < 0) {

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
}