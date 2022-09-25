using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BetterJoyForCemu {
	public static class Config { // stores dynamic configuration, including
		static readonly string path;
		static Dictionary<string, string> variables = new Dictionary<string, string>();

		const int settingsNum = 11; // currently - ProgressiveScan, StartInTray + special buttons

        static Config() {
            path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\settings";
        }

		public static string GetDefaultValue(string s) {
			switch (s) {
				case "ProgressiveScan":
					return "1";
				case "capture":
					return "key_" + ((int)WindowsInput.Events.KeyCode.PrintScreen);
				case "reset_mouse":
					return "joy_" + ((int)Joycon.Button.STICK);
			}
			return "0";
		}

		// Helper function to count how many lines are in a file
		// https://www.dotnetperls.com/line-count
		static long CountLinesInFile(string f) {
			// Zero based count
			long count = -1;
			using (StreamReader r = new StreamReader(f)) {
				string line;
				while ((line = r.ReadLine()) != null) {
					count++;
				}
			}
			return count;
		}

		public static void Init(Dictionary<string, Dictionary<string, int[]>> caliData) {
			foreach (string s in new string[] { "ProgressiveScan", "StartInTray", "capture", "home", "sl_l", "sl_r", "sr_l", "sr_r", "shake", "reset_mouse", "active_gyro" })
				variables[s] = GetDefaultValue(s);

			if (File.Exists(path)) {

				// Reset settings file if old settings
				if (CountLinesInFile(path) < settingsNum) {
					File.Delete(path);
					Init(caliData);
					return;
				}

				using (StreamReader file = new StreamReader(path)) {
					string line = String.Empty;
					int lineNO = 0;
					while ((line = file.ReadLine()) != null) {
						string[] vs = line.Split();
						try {
							if (lineNO < settingsNum) { // load in basic settings
								variables[vs[0]] = vs[1];
							} else { // load in calibration presets
								caliData[vs[0]].Clear();
								foreach (string caliDataStr in vs[1].Split('|')) {
									string[] caliArr = caliDataStr.Split(':');
									caliData[vs[0]][caliArr[0]] = caliArr[1].Split(',').Select(int.Parse).ToArray();
								}
							}
						} catch { }
						lineNO++;
					}
				}
			} else {
				using (StreamWriter file = new StreamWriter(path)) {
					foreach (string k in variables.Keys)
						file.WriteLine(String.Format("{0} {1}", k, variables[k]));
				}
			}
		}

		public static int IntValue(string key) {
			if (!variables.ContainsKey(key)) {
				return 0;
			}
			return Int32.Parse(variables[key]);
		}

		public static string Value(string key) {
			if (!variables.ContainsKey(key)) {
				return "";
			}
			return variables[key];
		}

		public static bool SetValue(string key, string value) {
			if (!variables.ContainsKey(key))
				return false;
			variables[key] = value;
			return true;
		}

		public static void SaveCaliData(Dictionary<string, Dictionary<string, int[]>> caliData) {
			string[] txt = File.ReadAllLines(path);
			if (txt.Length < settingsNum + 3) // no custom calibrations yet
				Array.Resize(ref txt, txt.Length + 3);

			int NO = settingsNum;
			foreach (KeyValuePair<string, Dictionary<string, int[]>> s_caliData in caliData) {
				txt[NO] = s_caliData.Key + " " + String.Join("|", s_caliData.Value.Select(
					sn_caliData => sn_caliData.Key + ':' + String.Join(",", sn_caliData.Value)
				));
				NO++;
			}
            File.WriteAllLines(path, txt);
		}

		public static void Save() {
			string[] txt = File.ReadAllLines(path);
			int NO = 0;
			foreach (string k in variables.Keys) {
				txt[NO] = String.Format("{0} {1}", k, variables[k]);
				NO++;
			}
			File.WriteAllLines(path, txt);
		}
	}
}
