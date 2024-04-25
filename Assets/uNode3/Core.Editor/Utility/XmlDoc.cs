using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace MaxyGames.UNode.Editors {
	/// <summary>
	/// Provides function to get Xml Documentation.
	/// </summary>
	public static class XmlDoc {
		static Dictionary<Type, XmlElement> cachedTypeXML = new Dictionary<Type, XmlElement>();
		static Dictionary<Assembly, XmlDocument> cacheAssembly = new Dictionary<Assembly, XmlDocument>();
		static Dictionary<MethodInfo, XmlElement> cachedMethodXML = new Dictionary<MethodInfo, XmlElement>();
		static Dictionary<MemberInfo, XmlElement> cachedMemberXML = new Dictionary<MemberInfo, XmlElement>();
		static Dictionary<ConstructorInfo, XmlElement> cachedConstructorXML = new Dictionary<ConstructorInfo, XmlElement>();
		private static Dictionary<string, string> XMLDocPath;

		/// <summary>
		/// Get the documentation for a MemberInfo
		/// </summary>
		/// <param name="memberInfo"></param>
		/// <returns></returns>
		public static string DocFromMember(MemberInfo memberInfo) {
			var documentation = XmlDoc.XMLFromMember(memberInfo);
			if(documentation != null && documentation["summary"] != null) {
				return documentation["summary"].InnerText.Trim();
			}
			return string.Empty;
		}

		/// <summary>
		/// Get Xml from MethodInfo
		/// </summary>
		/// <param name="methodInfo"></param>
		/// <returns></returns>
		public static XmlElement XMLFromMember(MethodInfo methodInfo) {
			if(!cachedMethodXML.ContainsKey(methodInfo)) {
				string parametersString = "";
				foreach(ParameterInfo parameterInfo in methodInfo.GetParameters()) {
					if(parametersString.Length > 0) {
						parametersString += ",";
					}

					parametersString += parameterInfo.ParameterType.FullName;
				}
				XmlElement xml;
				if(parametersString.Length > 0)
					xml = XMLFromName(methodInfo.DeclaringType, 'M', methodInfo.Name + "(" + parametersString + ")");
				else
					xml = XMLFromName(methodInfo.DeclaringType, 'M', methodInfo.Name);
				cachedMethodXML.Add(methodInfo, xml);
				return xml;
			}
			return cachedMethodXML[methodInfo];
		}

		/// <summary>
		/// Get Xml from ConstructorInfo
		/// </summary>
		/// <param name="ctor"></param>
		/// <returns></returns>
		public static XmlElement XMLFromMember(ConstructorInfo ctor) {
			if(!cachedConstructorXML.ContainsKey(ctor)) {
				string parametersString = "";
				foreach(ParameterInfo parameterInfo in ctor.GetParameters()) {
					if(parametersString.Length > 0) {
						parametersString += ",";
					}

					parametersString += parameterInfo.ParameterType.FullName;
				}
				XmlElement xml;
				if(parametersString.Length > 0)
					xml = XMLFromName(ctor.DeclaringType, 'M', "#ctor" + "(" + parametersString + ")");
				else
					xml = XMLFromName(ctor.DeclaringType, 'M', "#ctor");
				cachedConstructorXML.Add(ctor, xml);
				return xml;
			}
			return cachedConstructorXML[ctor];
		}

		/// <summary>
		/// Get Xml from MemberInfo
		/// </summary>
		/// <param name="memberInfo"></param>
		/// <returns></returns>
		public static XmlElement XMLFromMember(MemberInfo memberInfo) {
			if(memberInfo is IRuntimeMember) {
				return null;
			}
			if(memberInfo is MethodInfo) {
				return XMLFromMember(memberInfo as MethodInfo);
			} else if(memberInfo is ConstructorInfo) {
				return XMLFromMember(memberInfo as ConstructorInfo);
			} else if(memberInfo is Type) {
				return XMLFromType(memberInfo as Type);
			}
			if(!cachedMemberXML.ContainsKey(memberInfo)) {
				XmlElement xml = XMLFromName(memberInfo.DeclaringType, memberInfo.MemberType.ToString()[0], memberInfo.Name);
				cachedMemberXML.Add(memberInfo, xml);
				return xml;
			}
			return cachedMemberXML[memberInfo];
		}

		/// <summary>
		/// Get Xml from Type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static XmlElement XMLFromType(Type type) {
			if(!cachedTypeXML.ContainsKey(type)) {
				XmlElement xml = XMLFromName(type, 'T', "");
				cachedTypeXML.Add(type, xml);
			}
			return cachedTypeXML[type];
		}

		private static XmlElement XMLFromName(Type type, char prefix, string name) {
			string fullName;

			if(String.IsNullOrEmpty(name)) {
				fullName = prefix + ":" + type.FullName;
			} else {
				fullName = prefix + ":" + type.FullName + "." + name;
			}
			XmlDocument xmlDocument = XMLFromAssembly(type.Assembly);
			XmlElement matchedElement = null;
			if(xmlDocument != null) {
				try {
					foreach(var member in xmlDocument["doc"]["members"]) {
						if(member is XmlElement xmlElement) {
							if(xmlElement.Attributes["name"] != null && xmlElement.Attributes["name"].Value.Equals(fullName)) {
								if(matchedElement != null) {
									return null;
								}
								matchedElement = xmlElement;
							}
						}
						//else {
						//	Debug.Log(member.GetType());
						//}
					}
				}
				catch {
					return null;
				}
			}
			return matchedElement;
		}

		/// <summary>
		/// Get Xml from Assembly
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public static XmlDocument XMLFromAssembly(Assembly assembly) {
			if(assembly == null) {
				return null;
			}
			if(!cacheAssembly.ContainsKey(assembly)) {
				cacheAssembly[assembly] = XMLFromAssemblyNonCached(assembly);
			}
			return cacheAssembly[assembly];
		}

		private const string prefix = "file:///";
		private static XmlDocument XMLFromAssemblyNonCached(Assembly assembly) {
			if(assembly.CodeBase.StartsWith(prefix)) {
				string xml = null;
				if(XMLDocPath != null) {
					string path;
					if(XMLDocPath.TryGetValue(assembly.GetName().Name, out path)) {
						xml = File.ReadAllText(path);
					}
					else {
						if(File.Exists(Path.ChangeExtension(assembly.Location, ".xml"))) {
							xml = File.ReadAllText(Path.ChangeExtension(assembly.Location, ".xml"));
						}
					}
				}
				else {
					var assemblyName = assembly.GetName().Name;
					var dir = Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}{uNodePreference.preferenceDirectory}{Path.DirectorySeparatorChar}XML_Documentation{Path.DirectorySeparatorChar}";
					if(Directory.Exists(dir)) {
						var paths = Directory.GetFiles(dir);
						foreach(var p in paths) {
							if(p.EndsWith(".xml") && Path.GetFileNameWithoutExtension(p) == assemblyName) {
								xml = File.ReadAllText(p);
							}
						}
					}
					if(string.IsNullOrWhiteSpace(xml)) {
						TextAsset[] assets = Resources.LoadAll<TextAsset>("XML_Code_Documentation");
						foreach(TextAsset asset in assets) {
							string assetPath = AssetDatabase.GetAssetPath(asset);
							if(asset.name == assemblyName && assetPath.EndsWith(".xml") && File.Exists(assetPath)) {
								xml = File.ReadAllText(assetPath);
								break;
							}
						}
					}
					if(xml == null) {
						if(File.Exists(Path.ChangeExtension(assembly.Location, ".xml"))) {
							xml = File.ReadAllText(Path.ChangeExtension(assembly.Location, ".xml"));
						}
					}
				}
				if(xml != null) {
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(xml);
					return xmlDocument;
				}
			}
			return null;
		}

		public static Dictionary<string, string> FindXMLDocAsset() {
			if(XMLDocPath == null) {
				XMLDocPath = new Dictionary<string, string>();
				var dir = Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}Library{Path.DirectorySeparatorChar}UnityAssemblies{Path.DirectorySeparatorChar}";
				if(Directory.Exists(dir)) {
					var paths = Directory.GetFiles(dir);
					foreach(var p in paths) {
						if(p.EndsWith(".xml")) {
							XMLDocPath[Path.GetFileNameWithoutExtension(p)] = p;
						}
					}
				}
				TextAsset[] assets = Resources.LoadAll<TextAsset>("XML_Code_Documentation");
				foreach(TextAsset asset in assets) {
					string assetPath = AssetDatabase.GetAssetPath(asset);
					if(assetPath.EndsWith(".xml") && File.Exists(assetPath)) {
						XMLDocPath[asset.name] = assetPath;
						break;
					}
				}
				
				dir = Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}{uNodePreference.preferenceDirectory}{Path.DirectorySeparatorChar}XML_Documentation{Path.DirectorySeparatorChar}";
				if(Directory.Exists(dir)) {
					var paths = Directory.GetFiles(dir);
					foreach(var p in paths) {
						if(p.EndsWith(".xml")) {
							XMLDocPath[Path.GetFileNameWithoutExtension(p)] = p;
						}
					}
				}
			}
			return XMLDocPath;
		}

		/// <summary>
		/// Are the Xml Doc has loaded?
		/// </summary>
		public static bool hasLoadDoc;
		static bool isLoadDoc = false;
		static Thread loadDocThread;

		/// <summary>
		/// Load all Xml Doc in background.
		/// </summary>
		public static void LoadDocInBackground() {
			if(!hasLoadDoc && !isLoadDoc) {
				isLoadDoc = true;
				FindXMLDocAsset();
				loadDocThread = new Thread(new ThreadStart(DoLoadDoc));
				loadDocThread.IsBackground = true;
				loadDocThread.Start();
			}
		}

		/// <summary>
		/// Reload all Xml doc in background
		/// </summary>
		public static void ReloadDocInBackground() {
			if(isLoadDoc || loadDocThread != null) {
				loadDocThread.Abort();
			}
			hasLoadDoc = false;
			isLoadDoc = false;
			XMLDocPath = null;
			FindXMLDocAsset();
			LoadDocInBackground();
		}

		static void DoLoadDoc() {
			try {
				Assembly[] assemblies = EditorReflectionUtility.GetAssemblies();
				int index = 0;
				//uNodeThreadUtility.QueueAndWait(() => {
				//	EditorProgressBar.ShowProgressBar("Loading Documentation", 0);
				//});
				foreach(Assembly assembly in assemblies) {
					try {
						if(Thread.CurrentThread.ThreadState == ThreadState.AbortRequested) {
							break;
						}
						XMLFromAssembly(assembly);
						Thread.Sleep(1);
					}
					catch {
						continue;
					}
					index++;
					//uNodeThreadUtility.QueueAndWait(() => {
					//	EditorProgressBar.ShowProgressBar("Loading Documentation", (float)index / (float)assemblies.Length);
					//});
				}
			}
			catch { }
			hasLoadDoc = true;
			//uNodeThreadUtility.QueueAndWait(() => {
			//	EditorProgressBar.ClearProgressBar();
			//});
		}
	}
}