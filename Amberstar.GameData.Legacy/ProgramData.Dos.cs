using System.Text;
using Amber.Assets.Common;
using Amber.IO.Common.Serialization;
using Amber.Serialization;
using Amberstar.GameData.Legacy.Dos;
using Amberstar.GameData.Serialization;

namespace Amberstar.GameData.Legacy
{
	internal partial class ProgramData
	{
		/// <summary>
		/// The program data of the DOS version. Most of it is stored in the archives AMBEDATA.DAT and ICN_DATA.VGA,
		/// the rest in the data segment of GAME.EXE.
		/// </summary>
		public ProgramData(DosGameData dos)
		{
			Version = FindVersion(dos.Program);
			TextFragments = LoadTextFragments(dos);
			UITexts = LoadUITexts(dos);

			#region Graphics
			var windowTiles = dos.Read("ICN_DATA.VGA", "WINDOW.ICN");
			Windows.Add(0, new DataReader(CreateWindow(windowTiles, true)));
			Windows.Add(1, new DataReader(CreateWindow(windowTiles, false)));
			LayoutBottomCorners = [.. DosDefaults.LayoutBottomCorners];
			LayoutBottomCornerMasks = [.. DosDefaults.LayoutBottomCornerMasks];
			PortraitArea = CreatePortraitArea(dos.Read("ICN_DATA.VGA", "FRAMEKPF.ICN"));

			var frames = dos.Read("AMBEDATA.DAT", "FRAME.DAT");

			for (int i = 1; i <= 11; i++)
				Layouts.Add(i, new DataReader(frames[((i - 1) * 220)..(i * 220)]));

			var layoutBlocks = dos.Read("ICN_DATA.VGA", "LAYOUT.ICN");

			for (int i = 0; i < 132; i++)
				LayoutBlocks.Add(i, Graphic.FromBitPlanes(16, 16, DosGraphics.ChunkyToPlanar(layoutBlocks.AsSpan(i * 128, 128), 16, 16), 4));

			LoadUIGraphics(dos);

			// LOADING.ICN has the dwarf (128x112, the rest of the entry is unused) and COL_PALL.108 has the colors of
			// the palette of the Atari ST
			var loadingScreen = dos.TryRead("ICN_DATA.VGA", "LOADING.ICN");
			var loadingScreenPalette = dos.TryRead("AMBEDATA.DAT", "COL_PALL.108");

			if (loadingScreen != null && loadingScreenPalette != null && loadingScreen.Length >= GraphicLoader.LoadingScreenDataSize)
			{
				LoadingScreenData = DosGraphics.ChunkyToPlanar(loadingScreen.AsSpan(0, GraphicLoader.LoadingScreenDataSize),
					IGraphicLoader.LoadingScreenWidth, IGraphicLoader.LoadingScreenHeight);
				LoadingScreenPaletteData = DosGraphics.VgaToWidePalette(loadingScreenPalette);
			}
			#endregion

			#region Names
			var raceData = dos.Read("AMBEDATA.DAT", "RACE_DAT.DAT");
			var classData = dos.Read("AMBEDATA.DAT", "CLASSDAT.DAT");
			var miscData = dos.Read("AMBEDATA.DAT", "MISC_DAT.DAT");
			var spellData = dos.Read("AMBEDATA.DAT", "SPELLDAT.DAT");

			static void AddNames(Dictionary<int, IDataReader> names, byte[] data, int offset, int count, int firstKey = 0)
			{
				for (int i = 0; i < count; i++)
				{
					int position = offset + i * 2;
					var name = position + 2 <= data.Length ? data[position..(position + 2)] : [0, 0];
					names.Add(firstKey + i, new DataReader(DosConverter.Words(name)));
				}
			}

			AddNames(RaceNames, raceData, 330, 15);
			AddNames(AttributeNames, raceData, 360, 10);
			ClassData.AddRange(DosConverter.ClassData(classData));
			AddNames(ClassNames, classData, 4323, 11);
			AddNames(SkillNames, classData, 4345, 10);
			AddNames(CharInfoTexts, classData, 4365, 5);
			AddNames(LanguageNames, miscData, 0, 7);
			AddNames(ConditionNames, miscData, 16, 16, 1);
			AddNames(ItemTypeNames, miscData, 48, 19);
			SpellData = spellData[..Legacy.SpellData.TotalSize];
			AddNames(SpellSchoolNames, spellData, 840, 7, 1);
			AddNames(SpellNames, spellData, 854, 7 * 30, 1);
			AddNames(SpellLocationNames, spellData, 1274, 5);
			AddNames(SpellTargetNames, spellData, 1288, 8);
			#endregion

			#region Places and messages
			for (int i = 1; i <= 47; i++)
			{
				var place = dos.TryRead("AMBEDATA.DAT", $"PLACEDAT.{i:000}");

				if (place == null)
					break;

				var (data, name) = DosConverter.Place(place);
				PlacesData.Add(i, new DataReader(data));
				PlaceNames.Add(i, new DataReader(name));
			}

			MessageData = [.. Enumerable.Range(1, 9).Select(i => (IDataReader)new DataReader(DosConverter.Text(dos.Read("AMBEDATA.DAT", $"CODE{i:0000}.SYS"))))];
			#endregion

			#region Music
			var sound = dos.Archive("SOUND.MDI");
			int songIndex = 1;

			foreach (var entry in sound.Entries)
				Songs.Add(songIndex++, new DataReader(sound.Read(entry.Name)));

			SongMusicIndices = [.. DosDefaults.SongMusicIndices.Where(index => index <= Songs.Count)];

			// array[1..19] of string[15]
			for (int i = 0; i < SongMusicIndices.Count; i++)
				SongNames.Add(Encoding.Latin1.GetString(dos.DataSegmentString(0x664 + i * 16)));
			#endregion

			LoadCursors(dos.Read("ICN_DATA.VGA", "MAUSE.ICN"));
			ShadowData = dos.Read("AMBEDATA.DAT", "SHADOW.DAT");

			#region Combat
			CombatData = DosDefaults.CombatData;
			SpellAnimationData = DosDefaults.SpellAnimationData;
			DamageDigitData = DosDefaults.DamageDigitData;
			var monsterGraphicData = new List<byte>(DosDefaults.MonsterPositions);

			for (int i = 1; i <= 21; i++)
			{
				var info = dos.TryRead("COMBAT.VGA", $"MONDATA.{i:000}") ?? new byte[3];
				monsterGraphicData.AddRange([info[0], info[1], info[2], 0]);
			}

			MonsterGraphicData = [.. monsterGraphicData];
			#endregion

			AltarPuzzleData = LoadAltarPuzzle(dos);
		}

		static string FindVersion(byte[] program)
		{
			var marker = "V1."u8;
			int index = program.AsSpan().IndexOf(marker);

			if (index < 0)
				return "DOS";

			int end = index;

			while (end < program.Length && program[end] > ' ')
				end++;

			return "DOS " + Encoding.ASCII.GetString(program, index, end - index);
		}

		/// <summary>
		/// The dictionary (DICTIONA.GER or DICTIONA.ENG) has the same format as the text fragments of the
		/// Atari ST version, but the characters are stored as their code minus 32. The umlauts use the
		/// codes of [, \, ] and ^, which are converted to the codes of the Atari ST.
		/// </summary>
		static List<string> LoadTextFragments(DosGameData dos)
		{
			var data = dos.ReadFile(dos.German ? "DICTIONA.GER" : "DICTIONA.ENG");
			int count = data[0] | (data[1] << 8);
			int offset = 3 + data[2] - 1;
			var fragments = new List<string>(count) { "" };

			for (int i = 1; i < count && offset < data.Length; i++)
			{
				int length = data[offset++];
				var text = new byte[Math.Max(0, length - 1)];

				for (int c = 0; c < text.Length; c++)
					text[c] = ConvertCharacter((byte)(data[offset + c] + 32));

				offset += text.Length;
				fragments.Add(DataReader.Encoding.GetString(text));
			}

			return fragments;
		}

		/// <summary>
		/// The texts of the DOS version use [, \, ] and ^ for Ä, Ü, Ö and ß.
		/// </summary>
		static byte ConvertCharacter(byte character) => character switch
		{
			(byte)'[' => 0x8e,
			(byte)'\\' => 0x9a,
			(byte)']' => 0x99,
			(byte)'^' => 0x9e,
			_ => character
		};

		/// <summary>
		/// The UI texts have the colors of the Atari ST version (1 = ink, 2 = paper). Most texts are typed
		/// constants of GAME.EXE (array of string[11] at DS:08CE).
		/// </summary>
		static Dictionary<int, IDataReader> LoadUITexts(DosGameData dos)
		{
			string Constant(int index) => Encoding.Latin1.GetString(dos.DataSegmentString(0x8ce + index * 12));

			string Short(string text) => text.Length > 6 ? text[..5] + "." : text;

			const string Ink = "\u0001";
			const string Paper = "\u0002";
			const string Header = Ink + "\u0008";
			const string Normal = Ink + "\u000f";
			const string Title = Ink + "\u0001";

			var texts = new Dictionary<UIText, string>
			{
				{ UIText.Skills, Header + Constant(16) + Normal },
				{ UIText.Attributes, Header + Constant(14) + Normal },
				{ UIText.Age, Constant(2) + " " },
				{ UIText.Languages, Header + Constant(15) + Normal },
				{ UIText.Body, Header + Constant(17) + Normal },
				{ UIText.Mind, Header + Constant(18) + Normal },
				{ UIText.Weight, Normal + Paper + "\u0002" + Constant(3).TrimEnd() },
				{ UIText.Gold, Constant(22) },
				{ UIText.Food, Constant(23) },
				{ UIText.GoldFood, "G:01234 R:0123" },
				{ UIText.Male, Constant(0) },
				{ UIText.Female, Constant(1) },
				{ UIText.MaleShort, Short(Constant(0)) },
				{ UIText.FemaleShort, Short(Constant(1)) },
				{ UIText.Both, Constant(10) },
				{ UIText.Gender, Title + Constant(9) + ":" + Normal },
				{ UIText.Hands, Constant(4) + " " },
				{ UIText.Fingers, Constant(5) + " " },
				{ UIText.Classes, Title + "---- " + Constant(8) + " ----" + Normal },
				{ UIText.Damage, Constant(6) + " " },
				{ UIText.Protection, Constant(7) + " " },
				{ UIText.WeightWithColon, Title + Constant(3).TrimEnd() + " : " + Normal },
				{ UIText.Attribute, Title + Constant(11) + Normal },
				{ UIText.Skill, Title + Constant(12) + Normal },
				{ UIText.Magic, Title + Constant(13) + Normal },
				{ UIText.Cursed, Ink + "\u0009" + Constant(19) + Normal },
				{ UIText.Dialog, Normal + Paper + "ÿ" + (dos.German ? "DIALOG" : "DIALOGUE") },
				{ UIText.EnterWord, dos.German ? "GIB WORT EIN :" : "ENTER WORD :" },
			};

			// the texts with numbers are the same in all languages
			string[] numbers =
			[
				"EP:         ", "LP :    /   ", "SP :    /   ", "SLP:    ", ":   ", "  %/  %", "   /   ",
				Ink + "\u0000" + Paper + "\u0002" + "    KG/    KG", " GR", "LP-MAX:", "PP-MAX:", " M-B-W:", " M-B-A:", " : ", " (", "***", ")"
			];

			var result = new Dictionary<int, IDataReader>();
			int numberIndex = 0;

			foreach (var uiText in Enum.GetValues<UIText>())
			{
				string text = texts.TryGetValue(uiText, out var value) ? value : numbers[numberIndex++];
				result.Add((int)uiText, new DataReader([.. text.Select(c => (byte)c)]));
			}

			return result;
		}

		/// <summary>
		/// WINDOW.ICN has 8 tiles of 16x16 pixels: the upper left corner, the upper border, the upper right corner,
		/// the left and right border, the lower left corner, the lower border and the lower right corner.
		/// The borders only have a part of the tile, so a column (or row) of them is repeated.
		/// The windows of the Atari ST have 16 tiles (see Window) and the dark windows use the color 3 instead of 2.
		/// </summary>
		static byte[] CreateWindow(byte[] data, bool dark)
		{
			const int Fill = 2;
			var tiles = Enumerable.Range(0, 8).Select(i => DosGraphics.ChunkyToPixels(data.AsSpan(i * 128, 128), 16, 16)).ToArray();

			// empty pixels outside of the border stay transparent, the ones inside get the fill color
			byte Pixel(byte color, bool outside) => color != 0 ? color : outside ? (byte)0 : (byte)Fill;

			byte[] Horizontal(byte[] tile, bool top)
			{
				// the column with the most pixels
				int column = Enumerable.Range(0, 16).MaxBy(x => Enumerable.Range(0, 16).Count(y => tile[y * 16 + x] != 0));
				int first = Enumerable.Range(0, 16).First(y => tile[y * 16 + column] != 0);
				int last = Enumerable.Range(0, 16).Last(y => tile[y * 16 + column] != 0);
				var result = new byte[256];

				for (int y = 0; y < 16; y++)
				{
					byte color = Pixel(tile[y * 16 + column], top ? y < first : y > last);

					for (int x = 0; x < 16; x++)
						result[y * 16 + x] = color;
				}

				return result;
			}

			byte[] Vertical(byte[] tile, bool leftSide)
			{
				int row = Enumerable.Range(0, 16).MaxBy(y => Enumerable.Range(0, 16).Count(x => tile[y * 16 + x] != 0));
				int first = Enumerable.Range(0, 16).First(x => tile[row * 16 + x] != 0);
				int last = Enumerable.Range(0, 16).Last(x => tile[row * 16 + x] != 0);
				var result = new byte[256];

				for (int x = 0; x < 16; x++)
				{
					byte color = Pixel(tile[row * 16 + x], leftSide ? x < first : x > last);

					for (int y = 0; y < 16; y++)
						result[y * 16 + x] = color;
				}

				return result;
			}

			var upper = Horizontal(tiles[1], true);
			var lower = Horizontal(tiles[6], false);
			var left = Vertical(tiles[3], true);
			var right = Vertical(tiles[4], false);
			byte[][] window =
			[
				tiles[0], left, left, left, tiles[5],
				upper, lower, upper, lower, upper, lower,
				tiles[2], right, right, right, tiles[7]
			];
			var planar = new List<byte>(16 * 128);

			foreach (var tile in window)
			{
				var pixels = dark ? [.. tile.Select(p => p == Fill ? (byte)3 : p)] : tile;
				planar.AddRange(DosGraphics.PixelsToPlanar(pixels, 16, 16));
			}

			return [.. planar];
		}

		/// <summary>
		/// The area right of the portraits (16x36 pixels) with the status icon and the bars.
		/// </summary>
		static Graphic CreatePortraitArea(byte[] data)
		{
			Graphic Read(int offset, int width, int height) =>
				new(width, height, DosGraphics.ChunkyToPixels(data.AsSpan(offset, width * height / 2), width, height), GraphicFormat.PaletteIndices);

			// FRAMEKPF.ICN starts with 4 blocks of 16x36 pixels: the left end, two status blocks with a frame and
			// the stone between the portraits. Then the right end follows (32x36).
			var portraitArea = new Graphic(320, 37, GraphicFormat.PaletteIndices);
			var statusBlockLeft = Read(0, 16, 36);
			var statusBlockMid = Read(3 * 288, 16, 36);
			var statusBlockRight = Read(4 * 288, 32, 36);

			portraitArea.AddOverlay(0, 0, statusBlockLeft);

			for (int i = 1; i < 6; i++)
				portraitArea.AddOverlay(i * 48, 0, statusBlockMid);

			portraitArea.AddOverlay(6 * 48, 0, statusBlockRight);

			return portraitArea;
		}

		void LoadUIGraphics(DosGameData dos)
		{
			var special = dos.Read("ICN_DATA.VGA", "SPECIAL.ICN");
			var status = dos.Read("ICN_DATA.VGA", "STATUS.ICN");
			var control = dos.Read("ICN_DATA.VGA", "CONTROL.ICN");
			var control2 = dos.Read("ICN_DATA.VGA", "CONTROL2.ICN");
			var objects = dos.Read("ICN_DATA.VGA", "OBJECTS.ICN");

			void Add(UIGraphic graphic, byte[]? data, int offset)
			{
				var size = graphic.GetSize();
				int width = (int)size.Width;
				int height = (int)size.Height;
				int frameSize = width * height / 2;
				int frameCount = graphic.GetFrameCount();

				if (data == null || offset + frameCount * frameSize > data.Length)
				{
					UIGraphics.Add((int)graphic, new DataReader(new byte[frameCount * frameSize]));
					return;
				}

				UIGraphics.Add((int)graphic, new DataReader(DosGraphics.ChunkyToPlanar(data.AsSpan(offset), width, height, frameCount)));
			}

			Add(UIGraphic.Skull, dos.TryRead("ICN_DATA.VGA", "SKULL.ICN"), 0);
			Add(UIGraphic.FeedbackIcon, control2, 0);
			Add(UIGraphic.ChequeredIcon, control2, 256);

			int specialOffset = 0;

			for (var graphic = UIGraphic.Night; graphic <= UIGraphic.Invisibility; graphic++)
			{
				Add(graphic, special, specialOffset);
				var size = graphic.GetSize();
				specialOffset += (int)(size.Width * size.Height / 2);
			}

			Add(UIGraphic.EmptyItemSlot, null, 0);
			Add(UIGraphic.DamageSplash, status, 2560);
			Add(UIGraphic.HealingCross, status, 3072);
			Add(UIGraphic.Ouch, status, 3584);
			Add(UIGraphic.MagicAnimation, status, 4096);
			Add(UIGraphic.EmptyCharSlot, status, 5632);
			Add(UIGraphic.HPBar, null, 0);
			Add(UIGraphic.SPBar, null, 0);
			// The sword and the shield of the damage and protection values have 10x10 pixels and follow the two icons of
			// CONTROL2.ICN. The game uses 16x10 pixels, the rest is filled with the background color 2.
			void AddDamageProtectionIcon(UIGraphic graphic, int offset)
			{
				var pixels = new byte[16 * 10];
				Array.Fill(pixels, (byte)2);

				if (offset + 50 <= control2.Length)
				{
					var icon = DosGraphics.ChunkyToPixels(control2.AsSpan(offset, 50), 10, 10);

					for (int y = 0; y < 10; y++)
						icon.AsSpan(y * 10, 10).CopyTo(pixels.AsSpan(y * 16));
				}

				UIGraphics.Add((int)graphic, new DataReader(DosGraphics.PixelsToPlanar(pixels, 16, 10)));
			}

			AddDamageProtectionIcon(UIGraphic.Sword, 512);
			AddDamageProtectionIcon(UIGraphic.Shield, 512 + 50);
			var riddlemouth = dos.Read("ICN_DATA.VGA", "RM_ANIMA.ICN");
			Add(UIGraphic.RiddlemouthEyes, riddlemouth, 0);
			Add(UIGraphic.RiddlemouthMouth, riddlemouth, 4 * 80 * 29 / 2);
			Add(UIGraphic.CurseAnimation, dos.Read("ICN_DATA.VGA", "CURSEANI.ICN"), 0);
			// The automap icons of the DOS version have 8x8 pixels. The game only uses the left 8x8 pixels of the 16x8 icons.
			var automapIcons = dos.Read("ICN_DATA.VGA", "AUTOMAP.ICN");
			var automapData = new List<byte>();

			for (int i = 0; i < UIGraphic.AutomapIcons.GetFrameCount(); i++)
			{
				var pixels = new byte[16 * 8];

				if ((i + 1) * 32 <= automapIcons.Length)
				{
					var icon = DosGraphics.ChunkyToPixels(automapIcons.AsSpan(i * 32, 32), 8, 8);

					for (int y = 0; y < 8; y++)
						icon.AsSpan(y * 8, 8).CopyTo(pixels.AsSpan(y * 16));
				}

				automapData.AddRange(DosGraphics.PixelsToPlanar(pixels, 16, 8));
			}

			UIGraphics.Add((int)UIGraphic.AutomapIcons, new DataReader([.. automapData]));
			Add(UIGraphic.SmallOuch, null, 0);
			Add(UIGraphic.ItemSparkle, dos.Read("ICN_DATA.VGA", "MAGIC.ICN"), 0);

			// The button of the empty area is missing. It is the frame of a button.
			var firstButton = DosGraphics.ChunkyToPixels(control.AsSpan(0, 256), 32, 16);
			byte background = firstButton[2 + 1 * 32];

			for (int y = 1; y < 15; y++)
			{
				for (int x = 1; x < 31; x++)
					firstButton[y * 32 + x] = background;
			}

			Buttons.Add(0, new DataReader(DosGraphics.PixelsToPlanar(firstButton, 32, 16)));

			for (int i = 1; i <= (int)ButtonType.LastOriginalButton; i++)
			{
				int offset = (i - 1) * 256;
				var data = offset + 256 <= control.Length ? DosGraphics.ChunkyToPlanar(control.AsSpan(offset, 256), 32, 16) : new byte[256];
				Buttons.Add(i, new DataReader(data));
			}

			for (int i = 0; i <= (int)StatusIcon.LastStatusIcon; i++)
				StatusIcons.Add(i, new DataReader(DosGraphics.ChunkyToPlanar(status.AsSpan(i * 128, 128), 16, 16)));

			for (int i = 0; i <= (int)ItemGraphic.LastItemGraphic; i++)
			{
				var data = (i + 1) * 128 <= objects.Length ? DosGraphics.ChunkyToPlanar(objects.AsSpan(i * 128, 128), 16, 16) : new byte[128];
				ItemGraphics.Add(i, new DataReader(data));
			}

			// The sky has one gradient. The colors 160 to 243 of COL_PALL.104 (which starts at color 80) are the rows.
			var skyColors = dos.Read("AMBEDATA.DAT", "COL_PALL.104");

			for (int i = 0; i < 3; i++)
			{
				// day, night, dawn/dusk
				int brightness = i switch { 0 => 4, 1 => 1, _ => 2 };
				var gradient = new byte[84 * 2];

				for (int row = 0; row < 84; row++)
				{
					int color = (160 - 80 + row) * 3;
					int Component(int c) => Math.Min(7, skyColors[color + c] * brightness / 4 >> 3);
					gradient[row * 2] = (byte)Component(0);
					gradient[row * 2 + 1] = (byte)((Component(1) << 4) | Component(2));
				}

				SkyGradients.Add(i, new DataReader(gradient));
			}
		}

		/// <summary>
		/// The cursors of the DOS version have 4 bits per pixel (MAUSE.ICN). They are stored with their
		/// hotspot (2 words) and the 16x16 pixels in the order of the cursor types of the Atari ST.
		/// </summary>
		void LoadCursors(byte[] data)
		{
			int[] dosIndices = [21, 0, 2, 1, 3, 4, 5, 6, 7, 9, 11, 10, 12, 13, 14, 15, 16, 17, 18, 19, 8, 22, 22, 25, -1, 23, 24];

			for (int i = 0; i <= (int)CursorType.LastCursor; i++)
			{
				int dosIndex = dosIndices[i];
				var cursor = new byte[4 + 128];
				int hotspot = i == (int)CursorType.Sword ? 1 : 7;
				DosGraphics.WriteWord(cursor, 0, hotspot);
				DosGraphics.WriteWord(cursor, 2, hotspot);

				if (dosIndex >= 0 && (dosIndex + 1) * 128 <= data.Length)
					data.AsSpan(dosIndex * 128, 128).CopyTo(cursor.AsSpan(4));

				Cursors.Add(i, new DataReader(cursor));
			}
		}

		/// <summary>
		/// The altar puzzle data of GAME.EXE (DS:058E): the 12 text indices, the piece order, the piece count (word),
		/// the 12 x and 12 y coordinates (bytes) and the names (string[12]). The signs of the pieces are missing.
		/// </summary>
		static byte[] LoadAltarPuzzle(DosGameData dos)
		{
			const int Count = 12;
			const int Offset = 0x58e;
			var data = dos.DataSegment(Offset, 2 * Count + 2 + 2 * Count);

			if (data[2 * Count] != Count)
				return [];

			var result = new List<byte>();
			result.AddRange(data[..(2 * Count)].ToArray());
			result.AddRange(DosDefaults.AltarPuzzleSigns);

			for (int i = 0; i < Count; i++)
				result.AddRange([0, data[2 * Count + 2 + i], 0, data[3 * Count + 2 + i]]);

			result.AddRange(new byte[4 * Count]);

			for (int i = 0; i < Count; i++)
			{
				result.AddRange(dos.DataSegmentString(Offset + 2 * Count + 2 + 2 * Count + i * 13));
				result.Add(0);
			}

			return [.. result];
		}
	}
}
