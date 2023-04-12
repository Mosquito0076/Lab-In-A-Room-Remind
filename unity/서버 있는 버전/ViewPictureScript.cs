using UnityEngine;
using UnityEngine.UI;
using System;

public class ViewPictureScript : MonoBehaviour
{
    private Outline outline;
    private AllReportViewScript allReportViewScript;
    private ReportViewScript reportViewScript;
    public GameObject other;

    private void Awake()
    {
        allReportViewScript = other.GetComponent<AllReportViewScript>();
        reportViewScript = other.GetComponent<ReportViewScript>();
    }

    public void WhileHover()
    {
        if (allReportViewScript)
        {
            allReportViewScript.TogglePreview(Int32.Parse(name.Split("_")[1]));
        }
        else
        {
            reportViewScript.TogglePreview(Int32.Parse(name.Split("_")[1]));
        }
        outline = GetComponent<Outline>();
        outline.effectColor = Color.yellow;
        
    }

    public void WhenLeave()
    {
        if (allReportViewScript)
        {
            allReportViewScript.TogglePreview(-1);
        }
        else
        {
            reportViewScript.TogglePreview(-1);
        }
        outline = GetComponent<Outline>();
        outline.effectColor = Color.black;
    }
}
