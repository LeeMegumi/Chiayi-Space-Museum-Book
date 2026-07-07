using UnityEngine;

public class Main : MonoBehaviour
{
    public AutoFlip flip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            flip.FlipRightPage();
        }else if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            flip.FlipLeftPage();

        }
    }
}
