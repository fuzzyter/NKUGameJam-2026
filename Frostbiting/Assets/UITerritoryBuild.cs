// UITerritoryBuild.cs
using UnityEngine;
using UnityEngine.UI;


public class UITerritoryBuilder : MonoBehaviour
{
    public GameObject territorySprite; // Territory UI element
    public Color territoryColor = Color.green;// Color of the territory 
    private Image _territoyImage;

    public void DisplayTerritory()
      {
      // this checks if the obj exists to avoid NullReference error
      if (territorySprite != null)
        // updates territory
    {
    territorySprite.GetComponent<SpriteRenderer>().color = territoryColor;
    }
    else
    }
      Debug.LogWarning("Territory Sprite is missing");
    }
  }


{
  if (territorySprite != null)
    _territoryImage = territorySprite.GetComponent<Image>();
}
public void DisplayTerritory()
{
  if (_territoryImage != null)
  {
      if (territoryColor != Color.clear)
          _territoryImage.color = territoryColor;
  }
  else
    {
      Debug.LogWarning("Territory Sprite is missing");
    }
  }
}

  

