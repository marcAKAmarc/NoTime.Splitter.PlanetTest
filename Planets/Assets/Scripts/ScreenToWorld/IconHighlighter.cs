using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IconHighlighter : MonoBehaviour
{
    public List<Text> texts;
    private int originalSize;
    public int iconGrow = 2;

    public bool CanPickUp;
    public bool CanActivate;
    // Start is called before the first frame update
    void Start()
    {
        originalSize = texts[0].fontSize;
    }

    // Update is called once per frame
    void Update()
    {
        if (CanPickUp || CanActivate)
        {
            foreach (Text text in texts)
            {
                text.fontStyle = FontStyle.Bold;
                text.fontSize = originalSize + iconGrow;
            }
        }
        else
        {
            foreach (Text text in texts)
            {
                text.fontStyle = FontStyle.Normal;
                text.fontSize = originalSize;
            }
        }
    }
}
