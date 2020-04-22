﻿// The following code was generated by Microsoft Visual Studio 2005.
// The test owner should check each test for validity.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using CUETools.Codecs;
using CUETools.Codecs.Flake;
namespace CUETools.TestCodecs
{
	/// <summary>
	///This is a test class for CUETools.Codecs.Flake.FlakeWriter and is intended
	///to contain all CUETools.Codecs.Flake.FlakeWriter Unit Tests
	///</summary>
	[TestClass()]
	public class FlakeWriterTest
	{


		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}
		#region Additional test attributes
		// 
		//You can use the following additional attributes as you write your tests:
		//
		//Use ClassInitialize to run code before running the first test in the class
		//
		//[ClassInitialize()]
		//public static void MyClassInitialize(TestContext testContext)
		//{
		//}
		//
		//Use ClassCleanup to run code after all tests in a class have run
		//
		//[ClassCleanup()]
		//public static void MyClassCleanup()
		//{
		//}
		//
		//Use TestInitialize to run code before running each test
		//
		//[TestInitialize()]
		//public void MyTestInitialize()
		//{
		//}
		//
		//Use TestCleanup to run code after each test has run
		//
		//[TestCleanup()]
		//public void MyTestCleanup()
		//{
		//}
		//
		#endregion


		/// <summary>
		///A test for FlakeWriter (string, int, int, int, Stream)
		///</summary>
        [TestMethod()]
        public void ConstructorTest()
        {
            AudioBuffer buff = Codecs.WAV.AudioDecoder.ReadAllSamples(new Codecs.WAV.DecoderSettings(), "test.wav");
            AudioEncoder target;

            target = new AudioEncoder(new EncoderSettings() { PCM = buff.PCM, EncoderMode = "7" }, "Flakewriter0.flac");
            target.Settings.Padding = 1;
            target.DoSeekTable = false;
            //target.Vendor = "CUETools";
            //target.CreationTime = DateTime.Parse("15 Aug 1976");
            target.FinalSampleCount = buff.Length;
            target.Write(buff);
            target.Close();
            CollectionAssert.AreEqual(File.ReadAllBytes("Flake.flac"), File.ReadAllBytes("Flakewriter0.flac"), "Flakewriter0.flac doesn't match.");

            target = new AudioEncoder(new EncoderSettings() { PCM = buff.PCM, EncoderMode = "7" }, "Flakewriter1.flac");
            target.Settings.Padding = 1;
            target.DoSeekTable = false;
            //target.Vendor = "CUETools";
            //target.CreationTime = DateTime.Parse("15 Aug 1976");
            target.Write(buff);
            target.Close();
            CollectionAssert.AreEqual(File.ReadAllBytes("Flake.flac"), File.ReadAllBytes("Flakewriter1.flac"), "Flakewriter1.flac doesn't match.");
        }

		public static unsafe void
		compute_schur_reflection(/*const*/ double* autoc, uint max_order,
							  double* dreff/*[][MAX_LPC_ORDER]*/, double* err)
		{
			float* gen0 = stackalloc float[lpc.MAX_LPC_ORDER];
			float* gen1 = stackalloc float[lpc.MAX_LPC_ORDER];

			// Schur recursion
			for (uint i = 0; i < max_order; i++)
				gen0[i] = gen1[i] = (float)autoc[i + 1];
			float error = (float)autoc[0];

			for (uint i = 0; i < max_order; i++)
			{
				float reff = -gen1[0] / error;
				error += gen1[0] * reff; 

				for (uint j = 0; j < max_order - i - 1; j++)
				{
					gen1[j] = gen1[j + 1] + reff * gen0[j];
					gen0[j] = gen1[j + 1] * reff + gen0[j];
				}

				dreff[i] = reff;
				err[i] = error;
			}
		}


		[TestMethod()]
		public unsafe void LPCTest()
		{
			double* autoc = stackalloc double[9];
			double* reff = stackalloc double[8];
			double* err = stackalloc double[8];
			float* lpcs = stackalloc float[9 * lpc.MAX_LPC_ORDER];
			autoc[0] = 177286873088.0;
			autoc[1] = 177010016256.0;
			autoc[2] = 176182624256.0;
			autoc[3] = 174806581248.0;
			autoc[4] = 172888768512.0;
			autoc[5] = 170436820992.0;
			autoc[6] = 167460765696.0;
			autoc[7] = 163973169152.0;
			autoc[8] = 159987859456.0;

			compute_schur_reflection(autoc, 8, reff, err);
			lpc.compute_lpc_coefs(8, reff, lpcs);
			Assert.IsTrue(lpcs[7 * lpc.MAX_LPC_ORDER] < 3000);
		}

        [TestMethod()]
        public void SeekTest()
        {
            var r = new AudioDecoder(new DecoderSettings(), "test.flac");
            var buff1 = new AudioBuffer(r, 16536);
            var buff2 = new AudioBuffer(r, 16536);
            Assert.AreEqual(0, r.Position);
            r.Read(buff1, 7777);
            Assert.AreEqual(7777, r.Position);
            r.Position = 0;
            Assert.AreEqual(0, r.Position);
            r.Read(buff2, 7777);
            Assert.AreEqual(7777, r.Position);
            AudioBufferTest.AreEqual(buff1, buff2);
            r.Read(buff1, 7777);
            Assert.AreEqual(7777 + 7777, r.Position);
            r.Position = 7777;
            Assert.AreEqual(7777, r.Position);
            r.Read(buff2, 7777);
            Assert.AreEqual(7777 + 7777, r.Position);
            AudioBufferTest.AreEqual(buff1, buff2);
            r.Close();
        }
    }
}
