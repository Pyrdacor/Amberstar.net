using Amber.Common;
using Amber.Renderer.Common;
using Amberstar.Game.UI;
using Amberstar.GameData;
using Amberstar.GameData.Serialization;

namespace Amberstar.Game.Screens;

/// <summary>
/// The window to enter the name in the character editor.
/// </summary>
internal sealed class CharacterNameScreen : InputWordScreen
{
	public override ScreenType Type { get; } = ScreenType.InputCharacterName;

	// The text is only part of the program of the installer.
	protected override IText LabelText => Game.AssetProvider.TextLoader.FromString(
		Game.GetFullText(Game.LoadUIText(UIText.Male)) == "MALE" ? "ENTER NAME :" : "GIB NAME EIN :");

	protected override int? MaxInputLength => CharacterCreatorScreen.MaxNameLength;
}

/// <summary>
/// The character editor of a new game (Character editor module of INSTALL.PRG in the original).
/// The portrait, the gender and the name can be changed and the values can be rolled. A roll
/// sets the attributes and the first 8 skills to 1 up to their maximum, the gold to 1-100 and the food to 1-10.
/// </summary>
internal sealed class CharacterCreatorScreen : ButtonGridScreen
{
	public const int MaxNameLength = 15;
	const int PortraitButtonIndex = 0;
	const int OkButtonIndex = 2;
	const int NameButtonIndex = 3;
	const int RollButtonIndex = 6;
	const int FemaleButtonIndex = 7;
	const int MaleButtonIndex = 8;
	const int PortraitsPerGender = 6;
	const int RolledSkills = 8;
	const int MaxGold = 100;
	const int MaxFood = 10;
	const byte BackgroundDisplayLayer = 1;
	const byte TextDisplayLayer = 2;
	// above the portraits of the party
	const byte TitleDisplayLayer = 200;
	// between the sprite and the disabled overlay of a button
	const byte ButtonImageDisplayLayer = 12;
	const int NameColorIndex = 1;
	const int HeaderColorIndex = 8;
	const int TextColorIndex = 15;
	static readonly Rect statsArea = new(16, 49, 176, 144);
	static readonly Position portraitPosition = new(208, 49);
	IColoredRect? portraitBackground;
	IColoredRect? statsBackground;
	Image? portraitImage;
	IPartyMember hero = null!;
	string name = "";
	Gender gender;
	int portrait;
	Label? genderLabel;
	Label? nameLabel;
	Label? goldFoodLabel;
	readonly Label?[] attributeLabels = new Label?[8];
	readonly Label?[] skillLabels = new Label?[10];

	public override ScreenType Type { get; } = ScreenType.CharacterCreation;

	public override bool AllowInventoryAccess => false;

	public override bool AllowCharacterSelection => false;

	internal override byte ButtonGridPaletteIndex => Game?.PaletteIndexProvider.BuiltinPaletteIndices[BuiltinPalette.UI] ?? 0;

	public override void Open(Action? closeAction)
	{
		hero = Game.State.GetPartyMember(1)!;
		name = hero.Name;
		gender = hero.Gender;
		portrait = hero.CharacterCreatorPortrait ?? FirstPortrait(gender);

		base.Open(closeAction);

		var palette = ButtonGridPaletteIndex;
		Game.SetLayout(Layout.CharacterCreator, palette);
		Game.Cursor.CursorType = CursorType.Sword;
		Game.HidePartyMemberNames();

		// the title covers the portraits of the party
		portraitBackground = Game.CreateColoredRect(Layer.UI, new(0, 0), new(320, 37), Color.Black)!;
		portraitBackground.DisplayLayer = TitleDisplayLayer;
		var title = AddImage(0, 0, 320, 32, Game.GraphicIndexProvider.GetCharacterCreatorTitleIndex(), TitleDisplayLayer + 1, true);
		title.PaletteIndex = Game.PaletteIndexProvider.GetCharacterCreatorTitlePaletteIndex();

		statsBackground = Game.CreateColoredRect(Layer.UI, statsArea.Position, statsArea.Size, Game.PaletteColorProvider.GetPaletteColor(palette, 2))!;
		statsBackground.DisplayLayer = BackgroundDisplayLayer;

		portraitImage = AddImage(portraitPosition.X, portraitPosition.Y, 32, 34, Game.GraphicIndexProvider.GetCharacterCreatorPortraitIndex(portrait), TextDisplayLayer, true);
		portraitImage.PaletteIndex = palette;

		void AddButtonImage(int index, CharacterCreatorButton button)
		{
			var image = AddImage(ButtonGrid.OffsetX + index % 3 * Button.Width, ButtonGrid.OffsetY + index / 3 * Button.Height,
				Button.Width, Button.Height, Game.GraphicIndexProvider.GetCharacterCreatorButtonIndex(button), ButtonImageDisplayLayer, true);
			image.PaletteIndex = palette;
		}

		AddButtonImage(PortraitButtonIndex, CharacterCreatorButton.Portrait);
		AddButtonImage(OkButtonIndex, CharacterCreatorButton.Ok);
		AddButtonImage(NameButtonIndex, CharacterCreatorButton.Name);
		AddButtonImage(RollButtonIndex, CharacterCreatorButton.Roll);
		AddButtonImage(FemaleButtonIndex, CharacterCreatorButton.Female);
		AddButtonImage(MaleButtonIndex, CharacterCreatorButton.Male);

		AddText(16, 50, 176, Game.GetFullText(Game.LoadUIText(UIText.Attributes)), HeaderColorIndex, TextAlignment.Center);
		AddText(16, 115, 176, Game.GetFullText(Game.LoadUIText(UIText.Skills)), HeaderColorIndex, TextAlignment.Center);

		for (int i = 0; i < attributeLabels.Length; i++)
		{
			AddText(68, 57 + i * 7, 24, ShortName(AssetType.AttributeName, i));
			attributeLabels[i] = AddText(98, 57 + i * 7, 48, "");
		}

		for (int i = 0; i < skillLabels.Length; i++)
		{
			AddText(68, 122 + i * 7, 24, ShortName(AssetType.SkillName, i));
			skillLabels[i] = AddText(98, 122 + i * 7, 48, "");
		}

		genderLabel = AddText(242, 56, 62, "");
		AddText(242, 63, 62, $"{Game.GetFullText(Game.LoadUIText(UIText.Age)).TrimEnd()} {hero.Age.TotalCurrent}");
		nameLabel = AddText(208, 84, 96, "", NameColorIndex);
		goldFoodLabel = AddText(214, 118, 90, "");

		UpdateValues();
		UpdateGender();
		UpdateName();
	}

	public override void Close()
	{
		hero.Customize(name, gender, portrait);

		Game.Destroy(portraitBackground);
		Game.Destroy(statsBackground);
		portraitBackground = null;
		statsBackground = null;

		base.Close();

		Game.UpdatePartyMembers();
	}

	public override void ScreenPopped(Screen screen)
	{
		base.ScreenPopped(screen);

		if (screen is CharacterNameScreen && !string.IsNullOrWhiteSpace(Game.CurrentWord))
			SetName(Game.CurrentWord);
	}

	static int FirstPortrait(Gender gender) => gender == Gender.Male ? 0 : PortraitsPerGender;

	string ShortName(AssetType type, int index) => Game.AssetProvider.TextLoader.LoadText(new AssetIdentifier(type, index)).GetString()[..3];

	Label AddText(int x, int y, int width, string text, int colorIndex = TextColorIndex, TextAlignment alignment = TextAlignment.Left)
	{
		var label = AddLabel(x, y, width, 7, TextDisplayLayer);
		label.PaletteIndex = ButtonGridPaletteIndex;
		label.SetText(text, colorIndex, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		label.Alignment = alignment;
		label.Visible = true;
		return label;
	}

	void SetLabel(Label? label, string text, int colorIndex = TextColorIndex)
	{
		if (label == null)
			return;

		label.SetText(text, colorIndex, TextManager.TransparentPaper, ButtonGridPaletteIndex);
		label.Visible = true;
	}

	void SetName(string name)
	{
		this.name = name.Trim();

		if (this.name.Length > MaxNameLength)
			this.name = this.name[..MaxNameLength];

		UpdateName();
	}

	void UpdateName() => SetLabel(nameLabel, name, NameColorIndex);

	void UpdateGender()
	{
		SetLabel(genderLabel, Game.GetFullText(Game.LoadUIText(gender == Gender.Male ? UIText.Male : UIText.Female)));
		portraitImage?.SetTextureIndex(Game.GraphicIndexProvider.GetCharacterCreatorPortraitIndex(portrait));
		RequestButtonSetup();
	}

	void UpdateValues()
	{
		for (int i = 0; i < attributeLabels.Length; i++)
		{
			var attribute = hero.Attributes[(GameData.Attribute)(i + 1)];
			SetLabel(attributeLabels[i], $"{attribute.CurrentValue:000}/{attribute.MaxValue:000}");
		}

		for (int i = 0; i < skillLabels.Length; i++)
		{
			var skill = hero.Skills[(Skill)(i + 1)];
			SetLabel(skillLabels[i], $"{skill.TotalCurrent:00}%/{skill.MaxValue:00}%");
		}

		// G:01234 R:0123
		var text = Game.GetFullText(Game.LoadUIText(UIText.GoldFood));
		SetLabel(goldFoodLabel, $"{text[..2]}{hero.Gold:00000} {text[8..10]}{hero.Food:0000}");
	}

	static word Roll(int max) => (word)(max == 0 ? 0 : Game.Random(1, max));

	void RollValues()
	{
		for (int i = 1; i <= attributeLabels.Length; i++)
		{
			var attribute = hero.Attributes[(GameData.Attribute)i];
			attribute.CurrentValue = Roll(attribute.MaxValue);
		}

		for (int i = 1; i <= RolledSkills; i++)
		{
			var skill = hero.Skills[(Skill)i];
			skill.CurrentValue = Roll(skill.MaxValue);
		}

		hero.Gold = Roll(MaxGold);
		hero.Food = Roll(MaxFood);

		UpdateValues();
	}

	protected override void SetupButtons(ButtonGrid buttonGrid)
	{
		// The buttons of the original are shown above them.
		foreach (int index in new[] { PortraitButtonIndex, OkButtonIndex, NameButtonIndex, RollButtonIndex, FemaleButtonIndex, MaleButtonIndex })
			buttonGrid.SetButton(index, ButtonType.Exit);

		buttonGrid.EnableButton(FemaleButtonIndex, gender != Gender.Female);
		buttonGrid.EnableButton(MaleButtonIndex, gender != Gender.Male);
	}

	protected override void ButtonClicked(int index)
	{
		switch (index)
		{
			case PortraitButtonIndex:
				int first = FirstPortrait(gender);
				portrait = first + (portrait - first + 1) % PortraitsPerGender;
				UpdateGender();
				break;
			case OkButtonIndex:
				Game.ScreenHandler.PopScreen();
				break;
			case NameButtonIndex:
				Game.ScreenHandler.PushScreen(ScreenType.InputCharacterName);
				break;
			case RollButtonIndex:
				RollValues();
				break;
			case FemaleButtonIndex:
			case MaleButtonIndex:
				gender = index == MaleButtonIndex ? Gender.Male : Gender.Female;
				portrait = FirstPortrait(gender);
				UpdateGender();
				break;
		}
	}
}
