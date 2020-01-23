using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Dedicated to the dice object, defines its behavior
public class Dice : MonoBehaviour
{
    //Used to indicate a die is clickable by applying a highlight shader effect
    private const string HIGHLIGHTSHADERNAME = "Shader Graphs/DiceHighlight";
    private const string DEFAULTSHADERNAME = "Lightweight Render Pipeline/Lit";
    private bool clickable = false;

    //The game only supports rolls 1-6
    private int minDiceRollNum = 0;
    private int maxDiceRollNum = 6;

    //Force amount is the amount of force to use when simulating a dice roll
    [SerializeField] Vector3 forceAmount = new Vector3(-300f, 0f, 0f);
    [SerializeField] Rigidbody rb = null;
    [SerializeField] Renderer renderer = null;

    //After testing, these values were determined to produce the desired roll. Ex. if roll a 1 from RNG, then roll1Torque will be used to immitate a roll of 1
    //Easy way to still allow true randomness for the dice and to have a cool little visual of throwing dice
    private Vector3 roll1Torque = new Vector3(600f, 2000f, 4000f);
    private Vector3 roll2Torque = new Vector3(-30f, 360f, 360f);
    private Vector3 roll3Torque = new Vector3(-30f, -45f, -30f);
    private Vector3 roll4Torque = new Vector3(-180f, -10f, 10f);
    private Vector3 roll5Torque = new Vector3(1900f, 1100f, -2000f);
    private Vector3 roll6Torque = new Vector3(30f, -10f, 60f);

    //When given a number rolled, ex. 1, it will cause the dice to roll that value
    public void rollDice(int numToRoll) {
        if(numToRoll <= minDiceRollNum || numToRoll > maxDiceRollNum) {
            //Should not be possible, the number rolled should be in bounds
            Debug.LogError("Invalid number given to dice roll");
            return;
        }

        rb.velocity = new Vector3(0f, 0f, 0f);
        rb.angularVelocity = new Vector3(0f, 0f, 0f);

        rb.AddForce(forceAmount);

        if(numToRoll == 1) {
            rb.AddTorque(roll1Torque);
        }
        else if(numToRoll == 2) {
            rb.AddTorque(roll2Torque);
        }
        else if (numToRoll == 3) {
            rb.AddTorque(roll3Torque);
        }
        else if (numToRoll == 4) {
            rb.AddTorque(roll4Torque);
        }
        else if (numToRoll == 5) {
            rb.AddTorque(roll5Torque);
        }
        else if (numToRoll == 6) {
            rb.AddTorque(roll6Torque);
        }
        else {
            Debug.LogError("Invalid parameter given to dice roll");
        }
    }

    //This applys the shader to the dice and allows them to be clickable or nonclickable
    public void changeHighlightDice(bool toHighlight) {
        if (toHighlight) {
            renderer.material.shader = Shader.Find(HIGHLIGHTSHADERNAME);
            clickable = true;
        }
        else {
            renderer.material.shader = Shader.Find(DEFAULTSHADERNAME);
            clickable = false;
        }
    }

    public bool isClickable() {
        return clickable;
    }

}
