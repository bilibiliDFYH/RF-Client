namespace Rampastring.Tools;

using System.Collections.Generic;
using System.IO;

public interface IIniFile
{
    bool AllowNewSections { get; set; }
    string FileName { get; set; }

    IniFile AddSection(IniSection section);
    IniFile AddSection(string sectionName);
    IniFile RemoveSection(string sectionName);
    IniFile CombineSections(string firstSectionName, string secondSectionName);
    IniFile EraseSectionKeys(string sectionName);
    bool GetBooleanValue(string section, string key, bool defaultValue);
    double GetDoubleValue(string section, string key, double defaultValue);
    int GetIntValue(string section, string key, int defaultValue);
    string GetPathStringValue(string section, string key, string defaultValue);
    IniSection GetSection(string name);
    List<string> GetSectionKeys(string sectionName);
    List<string> GetSections();
    float GetSingleValue(string section, string key, float defaultValue);
    string GetStringValue(string section, string key, string defaultValue);
    string GetStringValue(string section, string key, string defaultValue, out bool success);
    bool KeyExists(string sectionName, string keyName);
    IniFile RemoveKey(string sectionName, string key);
    void Parse();
    IniFile Reload();
    bool SectionExists(string sectionName);
    IniFile SetBooleanValue(string section, string key, bool value);
    IniFile SetDoubleValue(string section, string key, double value);
    IniFile SetIntValue(string section, string key, int value);
    IniFile SetSingleValue(string section, string key, double value, int decimals);
    IniFile SetSingleValue(string section, string key, float value);
    IniFile SetSingleValue(string section, string key, float value, int decimals);
    IniFile SetStringValue(string section, string key, string value);
    IniFile WriteIniFile();
    IniFile WriteIniFile(string filePath);
    IniFile WriteIniStream(Stream stream);
}