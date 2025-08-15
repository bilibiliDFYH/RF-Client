using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using Ra2Client.Domain.Multiplayer;
using Ra2Client.DXGUI.Generic;

using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{
    public enum CheckBoxMapScoringMode
    {
        /// <summary>
        /// The value of the check box makes no difference for scoring maps.
        /// </summary>
        Irrelevant = 0,

        /// <summary>
        /// The check box prevents map scoring when it's checked.
        /// </summary>
        DenyWhenChecked = 1,

        /// <summary>
        /// The check box prevents map scoring when it's unchecked.
        /// </summary>
        DenyWhenUnchecked = 2
    }

    /// <summary>
    /// A game option check box for the game lobby.
    /// </summary>
    public class GameLobbyCheckBox : XNAClientCheckBox
    {
        public GameLobbyCheckBox(WindowManager windowManager) : base(windowManager) { }

        public bool IsMultiplayer { get; set; }

        /// <summary>
        /// The last host-defined value for this check box.
        /// Defaults to the default value of Checked after the check-box
        /// has been initialized, but its value is only changed by user interaction.
        /// </summary>
        public bool HostChecked { get; set; }

        /// <summary>
        /// The last value that the local player gave for this check box.
        /// Defaults to the default value of Checked after the check-box
        /// has been initialized, but its value is only changed by user interaction.
        /// </summary>
        public bool UserChecked { get; set; }

        /// <summary>
        /// The side indices that this check box disallows when checked.
        /// Defaults to -1, which means none.
        /// </summary>
        public List<int> DisallowedSideIndices = new List<int>();

        public bool AllowChanges { get; set; } = true;

        public CheckBoxMapScoringMode MapScoringMode { get; private set; } = CheckBoxMapScoringMode.Irrelevant;

        private string spawnIniOption;

        private string customIniPath;

        public bool Ares;



        private bool reversed;

        public bool Special { get; private set; } = false;

        public bool defaultValue;

        private string enabledSpawnIniValue = "True";
        private string disabledSpawnIniValue = "False";

        public List<string> ControlName;
        public List<string> ControlIndex;

        public bool basis = true;

        public override void Initialize()
        {
            // Find the game lobby that this control belongs to and register ourselves as a game option.

            XNAControl parent = Parent;
          
            while (true)
            {
                if (parent == null)
                    break;

                if(parent is CampaignSelector campaignSelector)
                {
                    if (campaignSelector.CheckBoxes.Find(chk => chk.Name == this.Name) == null)
                        campaignSelector.CheckBoxes.Add(this);
                    break;
                }

                // oh no, we have a circular class reference here!
                if (parent is GameLobbyBase gameLobby)
                {
                    if (gameLobby.CheckBoxes.Find(chk => chk.Name == this.Name) == null)
                        gameLobby.CheckBoxes.Add(this);
                    break;
                }

                parent = parent.Parent;
            }

            base.Initialize();
        }

        //protected override void OnEnabledChanged(object sender, EventArgs args)
        //{
        //    this.IdleColor = Color.White;
        //   // OnEnabledChanged(sender, args);
        //  //  this.OnEnabledChanged(sender, args);
        //}

        public override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "SpawnIniOption":
                    spawnIniOption = value;
                    return;
                case "EnabledSpawnIniValue":
                    enabledSpawnIniValue = value;
                    return;
                case "DisabledSpawnIniValue":
                    disabledSpawnIniValue = value;
                    return;
                case "CustomIniPath":
                    customIniPath = value;
                    return;
                case "Reversed":
                    reversed = Conversions.BooleanFromString(value, false);
                    return;
                case "CheckedMP":
                    if (IsMultiplayer)
                        Checked = Conversions.BooleanFromString(value, false);
                    return;
                case "Checked":
                    bool checkedValue = Conversions.BooleanFromString(value, false);
                    Checked = checkedValue;
                    defaultValue = checkedValue;
                    HostChecked = checkedValue;
                    UserChecked = checkedValue;
                    return;
                case "Special":
                    Special = bool.Parse(value);
                    return;
                case "Ares":
                    Ares = Conversions.BooleanFromString(value, false);
                    return;
                case "DisallowedSideIndex":
                case "DisallowedSideIndices":
                    List<int> sides = value.Split(',').ToList()
                        .Select(s => Conversions.IntFromString(s, -1)).Distinct().ToList();
                    DisallowedSideIndices.AddRange(sides.Where(s => !DisallowedSideIndices.Contains(s)));
                    return;
                case "MapScoringMode":
                    MapScoringMode = (CheckBoxMapScoringMode)Enum.Parse(typeof(CheckBoxMapScoringMode), value);
                    return;
                case "ControlName":
                    ControlName = value.Split(',').ToList();
                    return;
                case "ControlIndex":
                    ControlIndex = value.Split(',').ToList();
                    return;

            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        /// <summary>
        /// Applies the check-box's associated code to the spawn INI file.
        /// </summary>
        /// <param name="spawnIni">The spawn INI file.</param>
        public void ApplySpawnINICode(IniFile spawnIni)
        {
            if (string.IsNullOrEmpty(spawnIniOption))
                return;

            string value = disabledSpawnIniValue;
            if (Checked != reversed)
            {
                value = enabledSpawnIniValue;
            }

            spawnIni.SetValue("Settings", spawnIniOption, value);
        }

        /// <summary>
        /// Applies the check-box's associated code to the map INI file.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="gameMode">Currently selected gamemode, if set.</param>
        public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
        {
            if (Checked == reversed || String.IsNullOrEmpty(customIniPath))
                return;

            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
        }

        /// <summary>
        /// Applies the check-box's disallowed side index to a bool
        /// array that determines which sides are disabled.
        /// </summary>
        /// <param name="disallowedArray">An array that determines which sides are disabled.</param>
        public void ApplyDisallowedSideIndex(bool[] disallowedArray)
        {
            if (DisallowedSideIndices == null || DisallowedSideIndices.Count == 0)
                return;

            if (Checked != reversed)
            {
                for (int i = 0; i < DisallowedSideIndices.Count; i++)
                {
                    int sideNotAllowed = DisallowedSideIndices[i];
                    disallowedArray[sideNotAllowed] = true;
                }
            }
        }

        public override void OnLeftClick()
        {
            if (!AllowChanges)
                return;

            base.OnLeftClick();
            UserChecked = Checked;


        }
    }
}
