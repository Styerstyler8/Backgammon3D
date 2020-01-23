using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//The checker object will hold information and contain getters and setters needed for a selected checker
public class Checker : MonoBehaviour
{
    //Indicates if white checker or black checker
    [SerializeField] bool isWhite = false;

    //Deals with setting shader, to indicate when the checker is clickable to move
    private const string WHITEHIGHLIGHTSHADERNAME = "Shader Graphs/WhiteCheckerHighlight";
    private const string BLACKHIGHLIGHTSHADERNAME = "Shader Graphs/BlackCheckerHighlight";
    private const string DEFAULTSHADERNAME = "Lightweight Render Pipeline/Lit";
    private Renderer renderer = null;
    private bool clickable = false;

    //This will indicate what pointe the current checker is in, so when in pointe 0 will = 0
    private int currPointePos = -1;

    private void Awake() {
        renderer = GetComponent<Renderer>();
    }

    public bool isCheckerWhite() {
        return isWhite;
    }

    //Allows the checker to be clickable and sets the correct shader
    public void changeHighlightChecker(bool toHighlight) {
        if(toHighlight) {

            clickable = true;
            if (isWhite) {
                renderer.material.shader = Shader.Find(WHITEHIGHLIGHTSHADERNAME);
            }
            else {
                renderer.material.shader = Shader.Find(BLACKHIGHLIGHTSHADERNAME);
            }
        }
        else {
            renderer.material.shader = Shader.Find(DEFAULTSHADERNAME);
            clickable = false;
        }
    }

    public bool isClickable() {
        return clickable;
    }

    //Sets the pointePos to the new pointe pos the checker has been moved to
    public void setPos(int newPointePos) {
        currPointePos = newPointePos;
    }

    public int getPos() {
        if(currPointePos == -1) {
            Debug.Log("Error: Should not access checker without assigned pointe position");
        }

        return currPointePos;
    }
}
