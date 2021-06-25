using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using System.IO;
using System.Linq;

public class OrganizePokemonArt : EditorWindow
{
    int startingInteger = 001;
    int endingInteger = 001;
    [Tooltip("How many different Pokemon run from left to right")]
    int howManyPerLine = 10;
    public string[] pokemonNames;
    public int[] differentGenderArt;

    TextureImporter assetImporter = new TextureImporter();
    string assetPath;

    Vector2 pokemonArtMainSize = new Vector2(324, 195);
    Vector2 startingSlicingPos = new Vector2(0, 1);

    Vector2 pokemonArtStandardSize = new Vector2(80, 80);

    Vector2 pokemonFrontAPos = new Vector2(1, 82);
    Vector2 pokemonFrontBPos = new Vector2(82, 82);
    Vector2 pokemonBackAPos = new Vector2(163, 82);
    Vector2 pokemonBackBPos = new Vector2(244, 82);
    Vector2 pokemonShinyFrontAPos = new Vector2(1, 1);
    Vector2 pokemonShinyFrontBPos = new Vector2(82, 1);
    Vector2 pokemonShinyBackAPos = new Vector2(163, 1);
    Vector2 pokemonShinyBackBPos = new Vector2(244, 1);

    Vector2 pokemonArtSpriteSize = new Vector2(32, 32);

    Vector2 pokemonSpriteAPos = new Vector2(259, 163);
    Vector2 pokemonSpriteBPos = new Vector2(292, 163);

    Vector2 pokemonArtFootprintSize = new Vector2(16, 16);

    Vector2 pokemonFootprintPos = new Vector2(242, 163);

    [MenuItem("Tools/OrganizeNumbersForArt")]
    public static void ShowWindow()
    {
        GetWindow(typeof(OrganizePokemonArt));
    }

    void OnGUI()
    {
        GUILayout.Label("Organize Pokemon Art Name and Numbers", EditorStyles.boldLabel);
        startingInteger = EditorGUILayout.IntField("Starting Number: ", startingInteger);
        endingInteger = EditorGUILayout.IntField("Ending Number: ", endingInteger);

        ScriptableObject target = this;
        SerializedObject sO = new SerializedObject(target);
        pokemonNames = PokemonNameList.PokemonNameKanto1to151;
        differentGenderArt = PokemonNameList.PokemonKantoDifferentGenderSprites;
        SerializedProperty stringsProperty = sO.FindProperty("pokemonNames");
        SerializedProperty intProperty = sO.FindProperty("differentGenderArt");
        EditorGUILayout.PropertyField(stringsProperty, true);
        EditorGUILayout.PropertyField(intProperty, true);
        sO.ApplyModifiedProperties(); // Remember to apply modified properties

        if (GUILayout.Button("Organize Pokemon Selection"))
        {
            if (Selection.objects[0].GetType().Equals(typeof(Texture2D)))
            {
                Texture2D texture2 = Selection.objects[0] as Texture2D;

                ProcessTexture(texture2);
            }
        }
    }

    string Variant(int i)
    {
        switch (i)
        {
            case 0:
                return "FrontA";
            case 1:
                return "FrontB";
            case 2:
                return "BackA";
            case 3:
                return "BackB";
            default:
                Debug.Log("Broken");
                break;
        }
        return "Broken";
    }

    void ProcessTexture(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.spritePivot = Vector2.up;
        importer.maxTextureSize = 16384;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.Tight;
        textureSettings.spriteExtrude = 0;

        importer.SetTextureSettings(textureSettings);

        Rect[] mainImage = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, 1, 0);
        var rectsList = new List<Rect>(mainImage);
        rectsList = SliceUpPokemonToOwnRects(rectsList, texture.width, texture.height);
        rectsList = IndividualizeEachPokemonSlice(rectsList);

        string filenameNoExtension = Path.GetFileNameWithoutExtension(path);
        var metas = new List<SpriteMetaData>();

        int pokedexNumber = startingInteger;
        int currentVariant = 0;
        bool hasDifferentGenderArt = false;

        foreach (Rect rect in rectsList)
        {
            var meta = new SpriteMetaData();
            meta.pivot = Vector2.zero;
            meta.alignment = (int)SpriteAlignment.Center;
            meta.rect = rect;
            Debug.Log(pokemonNames[pokedexNumber - startingInteger]);
            string spriteName = $"{pokedexNumber.ToString("000")}_{pokemonNames[pokedexNumber - startingInteger]}_";

            if(currentVariant > 3 && currentVariant < 8)
            {
                spriteName += "Shiny_";
            }

            if(currentVariant == 0)
            {
                if (hasDifferentGenderArt == false && differentGenderArt.Contains(pokedexNumber) == true)
                {
                    hasDifferentGenderArt = true;
                }
                else
                {
                    hasDifferentGenderArt = false;
                }
            }

            if(differentGenderArt.Contains(pokedexNumber) == true && currentVariant < 8)
            {
                if(hasDifferentGenderArt == true)
                {
                    spriteName += "Male_";
                }
                else
                {
                    spriteName += "Female_";
                }
            }

            if(currentVariant < 8)
            {
                spriteName += Variant(currentVariant % 4);
            }
            else if(currentVariant == 8)
            {
                spriteName += "SpriteA";
            }
            else if (currentVariant == 9)
            {
                spriteName += "SpriteA";
            }
            else if (currentVariant == 10)
            {
                spriteName += "FootPrint";
            }

            currentVariant++;
            if (currentVariant >= 11)
            {
                pokedexNumber++;
                currentVariant = 0;
            }
            else if (currentVariant >= 8 && hasDifferentGenderArt == true)
            {
                currentVariant = 0;
            }
            
            meta.name = spriteName;
            metas.Add(meta);
        }

        importer.spritesheet = metas.ToArray();

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    List<Rect> SliceUpPokemonToOwnRects(List<Rect> rects, int textureWidth, int textureHeight)
    {
        List<Rect> list = new List<Rect>();

        for (int i = 0; i < (endingInteger + differentGenderArt.Length) - (startingInteger-1); i++)
        {
            int XPos = (int)startingSlicingPos.x + ((int)pokemonArtMainSize.x * (i % howManyPerLine));
            int YPos = textureHeight - (int)startingSlicingPos.y - ((int)pokemonArtMainSize.y * ((i / howManyPerLine) + 1));

            list.Add(new Rect(XPos, YPos, pokemonArtMainSize.x, pokemonArtMainSize.y));
        }

        return list;
    }

    List<Rect> IndividualizeEachPokemonSlice(List<Rect> slicedUpList)
    {
        List<Rect> list = new List<Rect>();
        int currentPokemon = 0;
        bool hasDifferentGenderArt = false;

        for (int i = 0; i < slicedUpList.Count; i++)
        {
            if(hasDifferentGenderArt == false && differentGenderArt.Contains(currentPokemon + startingInteger) == true)
            {
                hasDifferentGenderArt = true;
            }
            else
            {
                hasDifferentGenderArt = false;
            }

            //Regular

            int XPos = (int)slicedUpList[i].x + (int)pokemonFrontAPos.x;
            int YPos = (int)slicedUpList[i].y + (int)pokemonFrontAPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            XPos = (int)slicedUpList[i].x + (int)pokemonFrontBPos.x;
            YPos = (int)slicedUpList[i].y + (int)pokemonFrontBPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            XPos = (int)slicedUpList[i].x + (int)pokemonBackAPos.x;
            YPos = (int)slicedUpList[i].y + (int)pokemonBackAPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            XPos = (int)slicedUpList[i].x + (int)pokemonBackBPos.x;
            YPos = (int)slicedUpList[i].y + (int)pokemonBackBPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            //Shiny

            XPos = (int)slicedUpList[i].x + (int)pokemonShinyFrontAPos.x;
            YPos = (int)slicedUpList[i].y + (int)pokemonShinyFrontAPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            XPos = (int)slicedUpList[i].x + (int)pokemonShinyFrontBPos.x;
            YPos = (int)slicedUpList[i].y + (int)pokemonShinyFrontBPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            XPos = (int)slicedUpList[i].x + (int)pokemonShinyBackAPos.x;
            YPos = (int)slicedUpList[i].y + (int)pokemonShinyBackAPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            XPos = (int)slicedUpList[i].x + (int)pokemonShinyBackBPos.x;
            YPos = (int)slicedUpList[i].y + (int)pokemonShinyBackBPos.y;

            list.Add(new Rect(XPos, YPos, pokemonArtStandardSize.x, pokemonArtStandardSize.y));

            if(hasDifferentGenderArt == false)
            {
                //Sprite

                XPos = (int)slicedUpList[i].x + (int)pokemonSpriteAPos.x;
                YPos = (int)slicedUpList[i].y + (int)pokemonSpriteAPos.y;

                list.Add(new Rect(XPos, YPos, pokemonArtSpriteSize.x, pokemonArtSpriteSize.y));

                XPos = (int)slicedUpList[i].x + (int)pokemonSpriteBPos.x;
                YPos = (int)slicedUpList[i].y + (int)pokemonSpriteBPos.y;

                list.Add(new Rect(XPos, YPos, pokemonArtSpriteSize.x, pokemonArtSpriteSize.y));

                //Footprint

                XPos = (int)slicedUpList[i].x + (int)pokemonFootprintPos.x;
                YPos = (int)slicedUpList[i].y + (int)pokemonFootprintPos.y;

                list.Add(new Rect(XPos, YPos, pokemonArtFootprintSize.x, pokemonArtFootprintSize.y));

                currentPokemon++;
            }
        }

        return list;
    }
}