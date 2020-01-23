using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Dice manager will control the value dice will roll and control when it should roll the dice or not
public class DiceManager : MonoBehaviour
{
    //These define in game positions to place the dice when preparing to roll and when not rolling them
    [SerializeField] Vector3 diceThrowPos = Vector3.zero;
    [SerializeField] Vector3 diceResetPos = Vector3.zero;

    //References to the dice objects, the game supports 2 dice
    [SerializeField] Dice dice1 = null;
    [SerializeField] Dice dice2 = null;

    //Indicate seperation and resting rotation for dice
    [SerializeField] float dice2ZOffset = 2f;
    [SerializeField] float resetRotationY = 90f;

    private int minDiceRollNum = 1;
    private int maxDiceRollNum = 6;

    //When called, dice will move to resting position and be made clickable, indicating it is time to roll the dice
    public void resetDicePos() {
        resetTransformations(diceResetPos);
        this.transform.Rotate(0f, resetRotationY, 0f);
        makeDiceClickable();
    }

    //When called, will generate 2 random numbers and have the dice simulate a roll to land on these numbers
    //Have option for allowing doubles, useful for beginning of the game when doubles not allowed
    public int[] throwDice(bool allowDoubles) {
        resetTransformations(diceThrowPos); //Resets velocity to ensure throw isn't impacted
        dice1.changeHighlightDice(false); //Dice should not be allowed to be moved or selcted during throw
        dice2.changeHighlightDice(false);

        int diceValue1 = Random.Range(minDiceRollNum, maxDiceRollNum + 1);
        int diceValue2 = Random.Range(minDiceRollNum, maxDiceRollNum + 1);

        if(!allowDoubles) {
            while(diceValue1 == diceValue2) {
                diceValue1 = Random.Range(minDiceRollNum, maxDiceRollNum + 1);
                diceValue2 = Random.Range(minDiceRollNum, maxDiceRollNum + 1);
            }
        }

        dice1.rollDice(diceValue1); //Calls the dice to simulate the generated number roll
        dice2.rollDice(diceValue2);

        int[] rolledValues = { diceValue1, diceValue2 }; //Returns to the board the numbers rolled, in order to indicate which pieces are moveable
        return rolledValues;
    }

    //Resets the dice to ensure they have no weird position, velocity, or rotation
    //Ensures the rolls will give us what we want
    private void resetTransformations(Vector3 newPos) {
        this.transform.position = newPos;
        this.transform.rotation = Quaternion.identity;
        dice1.transform.localPosition = new Vector3(0f, 0f, 0f);
        dice2.transform.localPosition = new Vector3(0f, 0f, dice2ZOffset);
        dice1.transform.rotation = Quaternion.identity;
        dice2.transform.rotation = Quaternion.identity;
    }

    //Highlight the dice so that they are able to be selected
    private void makeDiceClickable() {
        dice1.changeHighlightDice(true);
        dice2.changeHighlightDice(true);
    }
}
