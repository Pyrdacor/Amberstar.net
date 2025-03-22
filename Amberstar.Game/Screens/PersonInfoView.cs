using Amber.Common;
using Amber.Renderer;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

internal class PersonInfoView
{
    readonly IColoredRect portraitBackground;
    readonly ISprite portrait;
    readonly IRenderText name;
    readonly IRenderText?[] personInfoTexts = new IRenderText?[4];
    bool visible = true;

    public PersonInfoView(Game game, IPerson person, int personIndex, byte palette)
    {
        var paperColor = TextManager.TransparentPaper;
        int portraitIndex = game.GraphicIndexProvider.GetPersonPortraitIndex(personIndex);

        var portraitPosition = new Position(208, 49);
        var portraitSize = new Size(32, 34);
        portraitBackground = game.CreateColoredRect(Layer.UI, portraitPosition, portraitSize, Color.Black)!;
        portrait = game.CreateSprite(Layer.UI, portraitPosition, portraitSize, portraitIndex, palette)!;
        portraitBackground!.DisplayLayer = 0;
        portrait!.DisplayLayer = 2;

        name = game.TextManager.Create(person.Name, 1, paperColor, palette);
        name.ShowInArea(portraitPosition.X, portraitPosition.Y + portraitSize.Height + 1, 16 * 6, 7, 2, TextAlignment.Center);

        var textLoader = game.AssetProvider.TextLoader;
        var textPositionX = portraitPosition.X + portraitSize.Width + 2;
        var textPositionY = portraitPosition.Y;
        int maxTextWidth = Game.VirtualScreenWidth - textPositionX;

        // Race
        var raceName = textLoader.LoadText(new AssetIdentifier(AssetType.RaceName, (int)person.Race));
        personInfoTexts[0] = game.TextManager.Create(raceName, maxTextWidth, 15, paperColor, palette);
        // Gender
        var genderName = person.Gender == Gender.Male ? "Männlich" : "Weiblich"; // TODO: Where to get the gender names from?
        personInfoTexts[1] = game.TextManager.Create(genderName, 15, paperColor, palette);
        // Age
        var ageString = $"Alter: {person.Age.CurrentValue}"; // TODO: Where to get the age label from?
        personInfoTexts[2] = game.TextManager.Create(ageString, 15, paperColor, palette);
        // Class + Level
        if (person.Class != Class.None)
        {
            var className = textLoader.LoadText(new AssetIdentifier(AssetType.ClassName, (int)person.Class));
            var classAndLevelString = textLoader.Concat(className, $" {person.Level}");
            personInfoTexts[3] = game.TextManager.Create(classAndLevelString, maxTextWidth, 15, paperColor, palette);
        }
        else
        {
            personInfoTexts[3] = null; // hidden
        }

        int lineHeight = personInfoTexts.FirstOrDefault(personInfoText => personInfoText != null)?.LineHeight ?? 0;

        for (int i = 0; i < personInfoTexts.Length; i++)
        {
            var text = personInfoTexts[i];
            text?.Show(textPositionX, textPositionY, 2);
            textPositionY += lineHeight;
        }
    }

    public bool Visible
    {
        get => visible;
        set
        {
            if (visible == value)
                return;

            visible = value;

            portraitBackground.Visible = value;
            portrait.Visible = value;
            name.Visible = value;

            for (int i = 0; i < personInfoTexts.Length; i++)
            {
                var text = personInfoTexts[i];

                if (text != null)
                    text.Visible = value;
            }
        }
    }

    public void Destroy()
    {
        portraitBackground.Visible = false;
        portrait.Visible = false;
        name.Delete();

        for (int i = 0; i < personInfoTexts.Length; i++)
        {
            var text = personInfoTexts[i];

            if (text != null)
                text.Delete();
        }
    }
}
