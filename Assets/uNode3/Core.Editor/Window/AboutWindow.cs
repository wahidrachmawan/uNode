﻿using UnityEngine;
using UnityEditor;

namespace MaxyGames.UNode.Editors {
	public class About : EditorWindow {
		public const string version = "3.2.4";
		public const float compilerVersion = 3.24f;

		GUIStyle PublisherNameStyle, headerStyle, infoStyle;
		GUIStyle ToolBarStyle;
		int ToolBarIndex, packageIndex;
		GUIContent[] toolbarOptions;
		GUIStyle GreyText;
		GUIStyle CenteredVersionLabel;
		GUIStyle ReviewBanner;
		GUILayoutOption BannerHeight;
		Vector2 scrool;

		bool StylesNotLoaded = true;

		[MenuItem("Tools/uNode/About", false, 5000000)]
		public static void ShowWindow() {
			About myWindow = GetWindow<About>(true, "About");
			myWindow.minSize = new Vector2(500, 400);
			myWindow.maxSize = myWindow.minSize;
			myWindow.titleContent = new GUIContent("About");
			myWindow.Show();
		}

		void OnEnable() {
			toolbarOptions = new GUIContent[3];
			toolbarOptions[0] = new GUIContent("<size=11><b> Support</b></size>\n <size=11>Get help and talk \n with others.</size>", Resources.Load<Texture2D>("Icons/support") as Texture2D, "");
			toolbarOptions[1] = new GUIContent("<size=11><b> Contact</b></size>\n <size=11>Reach out and \n get help.</size>", Resources.Load<Texture2D>("Icons/comments") as Texture2D, "");
			toolbarOptions[2] = new GUIContent("<size=11><b>  Third Party</b></size>\n <size=11>The third party \n software info.</size>", Resources.Load<Texture2D>("Icons/IconPackage") as Texture2D, "");
			BannerHeight = GUILayout.Height(30);
		}

		void LoadStyles() {
			PublisherNameStyle = new GUIStyle(EditorStyles.label) {
				alignment = TextAnchor.MiddleLeft,
				richText = true
			};

			headerStyle = new GUIStyle(EditorStyles.largeLabel) {
				fontStyle = FontStyle.Bold,
				richText = true
			};

			infoStyle = new GUIStyle(EditorStyles.label) {
				wordWrap = true,
			};

			ToolBarStyle = new GUIStyle("LargeButtonMid") {
				alignment = TextAnchor.MiddleLeft,
				richText = true,
			};
			ToolBarStyle.fixedHeight = 0;

			GreyText = new GUIStyle(EditorStyles.centeredGreyMiniLabel) {
				alignment = TextAnchor.MiddleLeft
			};

			CenteredVersionLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel) {
				alignment = TextAnchor.MiddleCenter,
			};

			ReviewBanner = new GUIStyle(EditorStyles.boldLabel) {
				alignment = TextAnchor.MiddleCenter,
				richText = true
			};

			StylesNotLoaded = false;
		}

		void OnGUI() {
			if(StylesNotLoaded)
				LoadStyles();

			EditorGUILayout.Space();
			GUILayout.Label(new GUIContent("<size=20><b><color=#666666>Wahid Rachmawan</color></b></size>"), PublisherNameStyle);
			EditorGUILayout.Space();

			var toolbarRect = uNodeGUIUtility.GetRectCustomHeight(50);
			ToolBarIndex = GUI.Toolbar(toolbarRect, ToolBarIndex, toolbarOptions, ToolBarStyle);

			switch(ToolBarIndex) {
				case 0:
					EditorGUILayout.Space();
					if(GUILayout.Button("Discord Server", EditorStyles.label))
						Application.OpenURL("https://discord.gg/8ufevvN");
					EditorGUILayout.LabelField("Joint uNode discord server and talk with others.", GreyText);
					if(GUILayout.Button("Support Forum", EditorStyles.label))
						Application.OpenURL("https://forum.unity.com/threads/released-unode-visual-scripting.500676/");
					EditorGUILayout.LabelField("Talk with others.", GreyText);

					EditorGUILayout.Space();
					if(GUILayout.Button("Documentation", EditorStyles.label))
						Application.OpenURL("https://docs.maxygames.com/unode/");
					EditorGUILayout.LabelField("Detailed documentation and quick-start guides.", GreyText);

					EditorGUILayout.Space();
					if(GUILayout.Button("YouTube Tutorials", EditorStyles.label))
						Application.OpenURL("https://youtube.com/@maxygames1?si=WIOzWhNwnhHvIKDm");
					EditorGUILayout.LabelField("Easy-to-digest tutorial videos and showcases.", GreyText);
					break;

				case 1:
					EditorGUILayout.Space();
					if(GUILayout.Button("Email", EditorStyles.label))
						Application.OpenURL("mailto:wahidrachmawan@yahoo.co.id?");
					EditorGUILayout.LabelField("Get in touch with me.", GreyText);
					break;
				case 2:
					EditorGUILayout.Space();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical(GUILayout.Width(150));
					packageIndex = GUILayout.SelectionGrid(packageIndex, 
						new GUIContent[] {
							new GUIContent("Roslyn"),
							new GUIContent("FatCow Icons"),
							new GUIContent("Odin Serializer"),
							new GUIContent("Contributors"),
						}, 1);
					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical();
					switch(packageIndex) {
						case 0:
							EditorGUILayout.Space();
							DrawTPInfo("Roslyn", "https://github.com/dotnet/roslyn", "MIT", @"The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.");
							break;
						case 1:
							DrawTPInfo("FatCow Icons", "http://www.fatcow.com/free-icons", "Creative Commons Attribution 3.0", "THE WORK (AS DEFINED BELOW) IS PROVIDED UNDER THE TERMS OF THIS CREATIVE COMMONS PUBLIC LICENSE (\"CCPL\" OR \"LICENSE\"). THE WORK IS PROTECTED BY COPYRIGHT AND/OR OTHER APPLICABLE LAW. ANY USE OF THE WORK OTHER THAN AS AUTHORIZED UNDER THIS LICENSE OR COPYRIGHT LAW IS PROHIBITED.\r\n" +
"BY EXERCISING ANY RIGHTS TO THE WORK PROVIDED HERE, YOU ACCEPT AND AGREE TO BE BOUND BY THE TERMS OF THIS LICENSE. TO THE EXTENT THIS LICENSE MAY BE CONSIDERED TO BE A CONTRACT, THE LICENSOR GRANTS YOU THE RIGHTS CONTAINED HERE IN CONSIDERATION OF YOUR ACCEPTANCE OF SUCH TERMS AND CONDITIONS.\r\n" +
"1. Definitions\r\n" +
"    \"Collective Work\" means a work, such as a periodical issue, anthology or encyclopedia, in which the Work in its entirety in unmodified form, along with one or more other contributions, constituting separate and independent works in themselves, are assembled into a collective whole. A work that constitutes a Collective Work will not be considered a Derivative Work (as defined below) for the purposes of this License.\r\n" +
"    \"Derivative Work\" means a work based upon the Work or upon the Work and other pre-existing works, such as a translation, musical arrangement, dramatization, fictionalization, motion picture version, sound recording, art reproduction, abridgment, condensation, or any other form in which the Work may be recast, transformed, or adapted, except that a work that constitutes a Collective Work will not be considered a Derivative Work for the purpose of this License. For the avoidance of doubt, where the Work is a musical composition or sound recording, the synchronization of the Work in timed-relation with a moving image (\"synching\") will be considered a Derivative Work for the purpose of this License.\r\n" +
"    \"Licensor\" means the individual, individuals, entity or entities that offers the Work under the terms of this License.\r\n" +
"    \"Original Author\" means the individual, individuals, entity or entities who created the Work.\r\n" +
"    \"Work\" means the copyrightable work of authorship offered under the terms of this License.\r\n" +
"    \"You\" means an individual or entity exercising rights under this License who has not previously violated the terms of this License with respect to the Work, or who has received express permission from the Licensor to exercise rights under this License despite a previous violation.\r\n" +
"2. Fair Use Rights. Nothing in this license is intended to reduce, limit, or restrict any rights arising from fair use, first sale or other limitations on the exclusive rights of the copyright owner under copyright law or other applicable laws.\r\n" +
"3. License Grant. Subject to the terms and conditions of this License, Licensor hereby grants You a worldwide, royalty-free, non-exclusive, perpetual (for the duration of the applicable copyright) license to exercise the rights in the Work as stated below:\r\n" +
"    to reproduce the Work, to incorporate the Work into one or more Collective Works, and to reproduce the Work as incorporated in the Collective Works;\r\n" +
"    to create and reproduce Derivative Works provided that any such Derivative Work, including any translation in any medium, takes reasonable steps to clearly label, demarcate or otherwise identify that changes were made to the original Work. For example, a translation could be marked \"The original work was translated from English to Spanish,\" or a modification could indicate \"The original work has been modified.\";;\r\n" +
"    to distribute copies or phonorecords of, display publicly, perform publicly, and perform publicly by means of a digital audio transmission the Work including as incorporated in Collective Works;\r\n" +
"    to distribute copies or phonorecords of, display publicly, perform publicly, and perform publicly by means of a digital audio transmission Derivative Works.\r\n" +
"    For the avoidance of doubt, where the Work is a musical composition:\r\n" +
"        Performance Royalties Under Blanket Licenses. Licensor waives the exclusive right to collect, whether individually or, in the event that Licensor is a member of a performance rights society (e.g. ASCAP, BMI, SESAC), via that society, royalties for the public performance or public digital performance (e.g. webcast) of the Work.\r\n" +
"        Mechanical Rights and Statutory Royalties. Licensor waives the exclusive right to collect, whether individually or via a music rights agency or designated agent (e.g. Harry Fox Agency), royalties for any phonorecord You create from the Work (\"cover version\") and distribute, subject to the compulsory license created by 17 USC Section 115 of the US Copyright Act (or the equivalent in other jurisdictions).\r\n" +
"    Webcasting Rights and Statutory Royalties. For the avoidance of doubt, where the Work is a sound recording, Licensor waives the exclusive right to collect, whether individually or via a performance-rights society (e.g. SoundExchange), royalties for the public digital performance (e.g. webcast) of the Work, subject to the compulsory license created by 17 USC Section 114 of the US Copyright Act (or the equivalent in other jurisdictions).\r\n" +
"The above rights may be exercised in all media and formats whether now known or hereafter devised. The above rights include the right to make such modifications as are technically necessary to exercise the rights in other media and formats. All rights not expressly granted by Licensor are hereby reserved.\r\n" +
"4. Restrictions. The license granted in Section 3 above is expressly made subject to and limited by the following restrictions:\r\n" +
"    You may distribute, publicly display, publicly perform, or publicly digitally perform the Work only under the terms of this License, and You must include a copy of, or the Uniform Resource Identifier for, this License with every copy or phonorecord of the Work You distribute, publicly display, publicly perform, or publicly digitally perform. You may not offer or impose any terms on the Work that restrict the terms of this License or the ability of a recipient of the Work to exercise the rights granted to that recipient under the terms of the License. You may not sublicense the Work. You must keep intact all notices that refer to this License and to the disclaimer of warranties. When You distribute, publicly display, publicly perform, or publicly digitally perform the Work, You may not impose any technological measures on the Work that restrict the ability of a recipient of the Work from You to exercise the rights granted to that recipient under the terms of the License. This Section 4(a) applies to the Work as incorporated in a Collective Work, but this does not require the Collective Work apart from the Work itself to be made subject to the terms of this License. If You create a Collective Work, upon notice from any Licensor You must, to the extent practicable, remove from the Collective Work any credit as required by Section 4(b), as requested. If You create a Derivative Work, upon notice from any Licensor You must, to the extent practicable, remove from the Derivative Work any credit as required by Section 4(b), as requested.\r\n" +
"    If You distribute, publicly display, publicly perform, or publicly digitally perform the Work (as defined in Section 1 above) or any Derivative Works (as defined in Section 1 above) or Collective Works (as defined in Section 1 above), You must, unless a request has been made pursuant to Section 4(a), keep intact all copyright notices for the Work and provide, reasonable to the medium or means You are utilizing: (i) the name of the Original Author (or pseudonym, if applicable) if supplied, and/or (ii) if the Original Author and/or Licensor designate another party or parties (e.g. a sponsor institute, publishing entity, journal) for attribution (\"Attribution Parties\") in Licensor's copyright notice, terms of service or by other reasonable means, the name of such party or parties; the title of the Work if supplied; to the extent reasonably practicable, the Uniform Resource Identifier, if any, that Licensor specifies to be associated with the Work, unless such URI does not refer to the copyright notice or licensing information for the Work; and, consistent with Section 3(b) in the case of a Derivative Work, a credit identifying the use of the Work in the Derivative Work (e.g., \"French translation of the Work by Original Author,\" or \"Screenplay based on original Work by Original Author\"). The credit required by this Section 4(b) may be implemented in any reasonable manner; provided, however, that in the case of a Derivative Work or Collective Work, at a minimum such credit will appear, if a credit for all contributing authors of the Derivative Work or Collective Work appears, then as part of these credits and in a manner at least as prominent as the credits for the other contributing authors. For the avoidance of doubt, You may only use the credit required by this Section for the purpose of attribution in the manner set out above and, by exercising Your rights under this License, You may not implicitly or explicitly assert or imply any connection with, sponsorship or endorsement by the Original Author, Licensor and/or Attribution Parties, as appropriate, of You or Your use of the Work, without the separate, express prior written permission of the Original Author, Licensor and/or Attribution Parties.\r\n" +
"5. Representations, Warranties and Disclaimer\r\n" +
"UNLESS OTHERWISE MUTUALLY AGREED TO BY THE PARTIES IN WRITING, LICENSOR OFFERS THE WORK AS-IS AND ONLY TO THE EXTENT OF ANY RIGHTS HELD IN THE LICENSED WORK BY THE LICENSOR. THE LICENSOR MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY KIND CONCERNING THE WORK, EXPRESS, IMPLIED, STATUTORY OR OTHERWISE, INCLUDING, WITHOUT LIMITATION, WARRANTIES OF TITLE, MARKETABILITY, MERCHANTIBILITY, FITNESS FOR A PARTICULAR PURPOSE, NONINFRINGEMENT, OR THE ABSENCE OF LATENT OR OTHER DEFECTS, ACCURACY, OR THE PRESENCE OF ABSENCE OF ERRORS, WHETHER OR NOT DISCOVERABLE. SOME JURISDICTIONS DO NOT ALLOW THE EXCLUSION OF IMPLIED WARRANTIES, SO SUCH EXCLUSION MAY NOT APPLY TO YOU.\r\n" +
"6. Limitation on Liability. EXCEPT TO THE EXTENT REQUIRED BY APPLICABLE LAW, IN NO EVENT WILL LICENSOR BE LIABLE TO YOU ON ANY LEGAL THEORY FOR ANY SPECIAL, INCIDENTAL, CONSEQUENTIAL, PUNITIVE OR EXEMPLARY DAMAGES ARISING OUT OF THIS LICENSE OR THE USE OF THE WORK, EVEN IF LICENSOR HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.\r\n" +
"7. Termination\r\n" +
"    This License and the rights granted hereunder will terminate automatically upon any breach by You of the terms of this License. Individuals or entities who have received Derivative Works (as defined in Section 1 above) or Collective Works (as defined in Section 1 above) from You under this License, however, will not have their licenses terminated provided such individuals or entities remain in full compliance with those licenses. Sections 1, 2, 5, 6, 7, and 8 will survive any termination of this License.\r\n" +
"    Subject to the above terms and conditions, the license granted here is perpetual (for the duration of the applicable copyright in the Work). Notwithstanding the above, Licensor reserves the right to release the Work under different license terms or to stop distributing the Work at any time; provided, however that any such election will not serve to withdraw this License (or any other license that has been, or is required to be, granted under the terms of this License), and this License will continue in full force and effect unless terminated as stated above.\r\n" +
"8. Miscellaneous\r\n" +
"    Each time You distribute or publicly digitally perform the Work (as defined in Section 1 above) or a Collective Work (as defined in Section 1 above), the Licensor offers to the recipient a license to the Work on the same terms and conditions as the license granted to You under this License.\r\n" +
"    Each time You distribute or publicly digitally perform a Derivative Work, Licensor offers to the recipient a license to the original Work on the same terms and conditions as the license granted to You under this License.\r\n" +
"    If any provision of this License is invalid or unenforceable under applicable law, it shall not affect the validity or enforceability of the remainder of the terms of this License, and without further action by the parties to this agreement, such provision shall be reformed to the minimum extent necessary to make such provision valid and enforceable.\r\n" +
"    No term or provision of this License shall be deemed waived and no breach consented to unless such waiver or consent shall be in writing and signed by the party to be charged with such waiver or consent.\r\n" +
"    This License constitutes the entire agreement between the parties with respect to the Work licensed here. There are no understandings, agreements or representations with respect to the Work not specified here. Licensor shall not be bound by any additional provisions that may appear in any communication from You. This License may not be modified without the mutual written agreement of the Licensor and You.\r\n");
							break;
						case 2:
							EditorGUILayout.Space();
							DrawTPInfo("Odin Serializer", "https://github.com/TeamSirenix/odin-serializer", "Apache License 2.0", @"                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      ""License"" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      ""Licensor"" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      ""Legal Entity"" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      ""control"" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      ""You"" (or ""Your"") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      ""Source"" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      ""Object"" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      ""Work"" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      ""Derivative Works"" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      ""Contribution"" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, ""submitted""
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as ""Not a Contribution.""

      ""Contributor"" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a ""NOTICE"" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an ""AS IS"" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS");
							break;
						case 3:
							EditorGUILayout.Space();
							DrawTPInfo("Contributors", "", "", @"Thanks to Jay wattashira for creating Logo and some of Icons.");
							break;
					}
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					break;
				default:
					break;
			}

			GUILayout.FlexibleSpace();

			string installedVersion = version;
#if UNODE_PRO
			installedVersion += " Pro";
#else
			installedVersion += " Free";
#endif
			EditorGUILayout.LabelField(new GUIContent("uNode v" + installedVersion), CenteredVersionLabel);
			EditorGUILayout.Space();
			if(GUILayout.Button(new GUIContent("<size=11> Please consider leaving us a review.</size>", Resources.Load<Texture2D>("Icons/award_star_gold_blue"), ""), ReviewBanner, BannerHeight))
				Application.OpenURL("https://assetstore.unity.com/packages/tools/visual-scripting/unode-3-pro-300160");
		}

		void DrawTPInfo(string title, string url, string license, string licenseDescription) {
			EditorGUI.LabelField(uNodeGUIUtility.GetRectCustomHeight(20), "<color=#444444>" + title  + "</color>", headerStyle);
			if(!string.IsNullOrEmpty(url)) {
				if(GUILayout.Button(url, EditorStyles.label))
					Application.OpenURL(url);
			}
			if(!string.IsNullOrEmpty(license))
				EditorGUILayout.LabelField("License: " + license, GreyText);
			scrool = EditorGUILayout.BeginScrollView(scrool, "Box");
			GUILayout.Box(licenseDescription, infoStyle);
			EditorGUILayout.EndScrollView();
		}
	}
}
