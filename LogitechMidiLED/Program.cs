using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;
using LedCSharp;
using System.Threading;

//Sanford's Midi Toolkit here which is required for this program to work can be found here: https://www.nuget.org/packages/Sanford.Multimedia.Midi/
//Or you can directly install it by running the following Nuget command: PM> Install-Package Sanford.Multimedia.Midi -Version 6.6.0
//Requires a Logitech Per Key RGB capable keyboard (e.g. G910, G810);

namespace LogitechMidiLED
{
    class Program
    {
        private static InputDevice inDevice = null;
        private const int SysExBufferSize = 128;
        public static KeyData[] KD = new KeyData[128];
        public static Boolean ReadyToTakeOff = true;

        static void Main(string[] args)
        {
            Console.Title = "Logitech Midi Lighting Public Demo v1.0";
            Console.WriteLine("Logitech Midi Lighting Public Demo v1.0. Press Enter to begin setup");
            Console.ReadLine();
            int InitCount = 0;

            while (true)
            {
                Console.Write("Logitech LED initializing...");
                Boolean Initialized = LogitechGSDK.LogiLedInit();
                LogitechGSDK.LogiLedSetTargetDevice(LogitechGSDK.LOGI_DEVICETYPE_PERKEY_RGB);
                if (Initialized)
                    break;

                Console.WriteLine("Failed! Retrying..." + InitCount);
                InitCount++;
                Thread.Sleep(1000);
            }

            LogitechGSDK.LogiLedSetLighting(100, 0, 0);
            KD = initializeMidiKeyboardMapping(KD);
            

            Thread StandbyLighting = new Thread(FlashReadyLight);
            StandbyLighting.Start();

            Console.WriteLine("LED Ready! initializing MIDI...");
            Boolean MidiSuccess = false;
            do
            {
                if (InputDevice.DeviceCount == 0)
                {
                    Console.WriteLine("No MIDI input devices available. Press enter to try again!");
                    Console.ReadLine();
                }
                else
                {
                    try
                    {
                        inDevice = new InputDevice(0);
                        inDevice.ChannelMessageReceived += HandleChannelMessageReceived;
                        MidiSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error! " + ex.ToString());
                        Console.ReadLine();
                    }
                }

            } while (!MidiSuccess);

            Console.WriteLine("Midi Reader Ready! Press \"ENTER\" to Begin...");
            Console.ReadLine();
            StandbyLighting.Abort();

            for (int i = 0; i < KD.Length; i++)
            {
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(KD[i].Key, 0, 0, 0);
                Thread.Sleep(10);
            }

            inDevice.StartRecording();
            Console.WriteLine("LED System is now Running. Press \"ENTER\" to exit...");
            Console.ReadLine();

            Console.WriteLine("Program Shutting Down...");
            LogitechGSDK.LogiLedStopEffects();
            LogitechGSDK.LogiLedShutdown();
            inDevice.StopRecording();
            inDevice.Close();
        }

        public static void FlashReadyLight()    //Loop that flashes both Return and Num_Pad Ready buttons
        {
            LogitechGSDK.LogiLedSetLighting(0, 25, 0);
            while (ReadyToTakeOff)
            {
                Thread.Sleep(500);
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(keyboardNames.ENTER, 0, 100, 0);
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(keyboardNames.NUM_ENTER, 0, 100, 0);
                Thread.Sleep(500);
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(keyboardNames.ENTER, 0, 25, 0);
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(keyboardNames.NUM_ENTER, 0, 25, 0);
            }
        }

        private static void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            /*  Code to see what is being sent over the Midi protocol. Useful for debugging
            Console.WriteLine(
                    e.Message.Command.ToString() + '\t' + '\t' +    //Type of Midi Command (e.g. NoteOn, NoteOff, Controller, etc...)
                    e.Message.MidiChannel.ToString() + '\t' +       //Midi Channel Number the command belongs to.
                    e.Message.Data1.ToString() + '\t' +             //Note Number. This is the value that is used to determine which key on our keyboard we are going to light up. (Values from 0-127)
                    e.Message.Data2.ToString());                    //Intensity number, basically, how hard the key is being hit as defined in the Midi.(Values from 0-127)
                    */
            SetKeyColourForNote(e.Message.Command.ToString(), e.Message.MidiChannel, e.Message.Data1, e.Message.Data2, KD);
        }

        public static void SetKeyColourForNote(String ChannelMode, int Channel, int Data1, int Data2, KeyData[] KD)
        {
            
            int IntensityPercentage = (int)((Data2 / 128.00) * 100.00); //Since Logitech's LED SDK only accepts Percentage range from 0% to 100%, 
                                                                        //we have to convert our Data2 value from 128 based to 100% based
            keyboardNames KeyName = KD[Data1].Key;

            Random random = new Random();   //Randomizes 3 sets of different numbers between 1 and the Midi's Intensity value.
            int RandomR = random.Next(0, IntensityPercentage) + 1,
                RandomG = random.Next(0, IntensityPercentage) + 1, 
                RandomB = random.Next(0, IntensityPercentage) + 1;

            if (ChannelMode == "NoteOff" || (ChannelMode == "NoteOn" && Data2 == 0)) //Some Midi programs use NoteOn with Intensity of 0 instead of NoteOff so an "or" case is used here.
            {
                Console.WriteLine("CH: " + Channel + " State: OFF - Key: " + KeyName + "");
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(KeyName, 0, 0, 0);
            }
            else if (ChannelMode == "NoteOn")
            {
                Console.WriteLine("CH: " + Channel + " State: ON  - Key: " + KeyName + " \t- " + RandomR + "% Red, " + RandomG + "% Green, " + RandomB + "% Blue at " + IntensityPercentage + "% Intensitiy");
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(KeyName, RandomR, RandomG, RandomB);
            }
            
        }

        public static KeyData[] initializeMidiKeyboardMapping(KeyData[] KD)
        {
            for (int i = 0; i < KD.Length; i++)
            {
                KD[i] = new KeyData();
                KD[i].Key = keyboardNames.G_LOGO; //Default maps to Logitech Logo
                KD[i].isCaps = false; 
            }

            #region Mapping Midi data1 values to the Keys

            keyboardNames[] KBN = new keyboardNames[114];
            List<keyboardNames> KBSeqList = new List<keyboardNames>();
            
            #region mapping keys in sequential order from top right, top left to bottom right, bottom left.
            KBSeqList.Add(keyboardNames.ESC);
            KBSeqList.Add(keyboardNames.F1);
            KBSeqList.Add(keyboardNames.F2);
            KBSeqList.Add(keyboardNames.F3);
            KBSeqList.Add(keyboardNames.F4);
            KBSeqList.Add(keyboardNames.F5);
            KBSeqList.Add(keyboardNames.F6);
            KBSeqList.Add(keyboardNames.F7);
            KBSeqList.Add(keyboardNames.F8);
            KBSeqList.Add(keyboardNames.F9);
            KBSeqList.Add(keyboardNames.F10);
            KBSeqList.Add(keyboardNames.F11);
            KBSeqList.Add(keyboardNames.F12);
            KBSeqList.Add(keyboardNames.PRINT_SCREEN);
            KBSeqList.Add(keyboardNames.SCROLL_LOCK);
            KBSeqList.Add(keyboardNames.PAUSE_BREAK);
            KBSeqList.Add(keyboardNames.TILDE);
            KBSeqList.Add(keyboardNames.ONE);
            KBSeqList.Add(keyboardNames.TWO);
            KBSeqList.Add(keyboardNames.THREE);
            KBSeqList.Add(keyboardNames.FOUR);
            KBSeqList.Add(keyboardNames.FIVE);
            KBSeqList.Add(keyboardNames.SIX);
            KBSeqList.Add(keyboardNames.SEVEN);
            KBSeqList.Add(keyboardNames.EIGHT);
            KBSeqList.Add(keyboardNames.NINE);
            KBSeqList.Add(keyboardNames.ZERO);
            KBSeqList.Add(keyboardNames.MINUS);
            KBSeqList.Add(keyboardNames.EQUALS);
            KBSeqList.Add(keyboardNames.BACKSPACE);
            KBSeqList.Add(keyboardNames.INSERT);
            KBSeqList.Add(keyboardNames.HOME);
            KBSeqList.Add(keyboardNames.PAGE_UP);
            KBSeqList.Add(keyboardNames.NUM_LOCK);
            KBSeqList.Add(keyboardNames.NUM_SLASH);
            KBSeqList.Add(keyboardNames.NUM_ASTERISK);
            KBSeqList.Add(keyboardNames.NUM_MINUS);
            KBSeqList.Add(keyboardNames.TAB);
            KBSeqList.Add(keyboardNames.Q);
            KBSeqList.Add(keyboardNames.W);
            KBSeqList.Add(keyboardNames.E);
            KBSeqList.Add(keyboardNames.R);
            KBSeqList.Add(keyboardNames.T);
            KBSeqList.Add(keyboardNames.Y);
            KBSeqList.Add(keyboardNames.U);
            KBSeqList.Add(keyboardNames.I);
            KBSeqList.Add(keyboardNames.O);
            KBSeqList.Add(keyboardNames.P);
            KBSeqList.Add(keyboardNames.OPEN_BRACKET);
            KBSeqList.Add(keyboardNames.CLOSE_BRACKET);
            KBSeqList.Add(keyboardNames.BACKSLASH);
            KBSeqList.Add(keyboardNames.KEYBOARD_DELETE);
            KBSeqList.Add(keyboardNames.END);
            KBSeqList.Add(keyboardNames.PAGE_DOWN);
            KBSeqList.Add(keyboardNames.NUM_SEVEN);
            KBSeqList.Add(keyboardNames.NUM_EIGHT);
            KBSeqList.Add(keyboardNames.NUM_NINE);
            KBSeqList.Add(keyboardNames.NUM_PLUS);
            KBSeqList.Add(keyboardNames.CAPS_LOCK);
            KBSeqList.Add(keyboardNames.A);
            KBSeqList.Add(keyboardNames.S);
            KBSeqList.Add(keyboardNames.D);
            KBSeqList.Add(keyboardNames.F);
            KBSeqList.Add(keyboardNames.G);
            KBSeqList.Add(keyboardNames.H);
            KBSeqList.Add(keyboardNames.J);
            KBSeqList.Add(keyboardNames.K);
            KBSeqList.Add(keyboardNames.L);
            KBSeqList.Add(keyboardNames.SEMICOLON);
            KBSeqList.Add(keyboardNames.APOSTROPHE);
            KBSeqList.Add(keyboardNames.ENTER);
            KBSeqList.Add(keyboardNames.NUM_FOUR);
            KBSeqList.Add(keyboardNames.NUM_FIVE);
            KBSeqList.Add(keyboardNames.NUM_SIX);
            KBSeqList.Add(keyboardNames.LEFT_SHIFT);
            KBSeqList.Add(keyboardNames.Z);
            KBSeqList.Add(keyboardNames.X);
            KBSeqList.Add(keyboardNames.C);
            KBSeqList.Add(keyboardNames.V);
            KBSeqList.Add(keyboardNames.B);
            KBSeqList.Add(keyboardNames.N);
            KBSeqList.Add(keyboardNames.M);
            KBSeqList.Add(keyboardNames.COMMA);
            KBSeqList.Add(keyboardNames.PERIOD);
            KBSeqList.Add(keyboardNames.FORWARD_SLASH);
            KBSeqList.Add(keyboardNames.RIGHT_SHIFT);
            KBSeqList.Add(keyboardNames.ARROW_UP);
            KBSeqList.Add(keyboardNames.NUM_ONE);
            KBSeqList.Add(keyboardNames.NUM_TWO);
            KBSeqList.Add(keyboardNames.NUM_THREE);
            KBSeqList.Add(keyboardNames.NUM_ENTER);
            KBSeqList.Add(keyboardNames.LEFT_CONTROL);
            KBSeqList.Add(keyboardNames.LEFT_WINDOWS);
            KBSeqList.Add(keyboardNames.LEFT_ALT);
            KBSeqList.Add(keyboardNames.SPACE);
            KBSeqList.Add(keyboardNames.RIGHT_ALT);
            KBSeqList.Add(keyboardNames.RIGHT_WINDOWS);
            KBSeqList.Add(keyboardNames.APPLICATION_SELECT);
            KBSeqList.Add(keyboardNames.RIGHT_CONTROL);
            KBSeqList.Add(keyboardNames.ARROW_LEFT);
            KBSeqList.Add(keyboardNames.ARROW_DOWN);
            KBSeqList.Add(keyboardNames.ARROW_RIGHT);
            KBSeqList.Add(keyboardNames.NUM_ZERO);
            KBSeqList.Add(keyboardNames.NUM_PERIOD);
            KBSeqList.Add(keyboardNames.G_1);
            KBSeqList.Add(keyboardNames.G_2);
            KBSeqList.Add(keyboardNames.G_3);
            KBSeqList.Add(keyboardNames.G_4);
            KBSeqList.Add(keyboardNames.G_5);
            KBSeqList.Add(keyboardNames.G_6);
            KBSeqList.Add(keyboardNames.G_7);
            KBSeqList.Add(keyboardNames.G_8);
            KBSeqList.Add(keyboardNames.G_9);
            KBSeqList.Add(keyboardNames.G_LOGO);
            KBSeqList.Add(keyboardNames.G_BADGE);
            #endregion
            
            int KBNIndex = 13;  //Since there are more possible note numbers than the amount of LED keys we can assign them to, 
                                //we're just going to ignore the first 13 values from the midi channel instead.

            foreach (keyboardNames keyboard in KBSeqList)   //Maps the keyboard names to the index number that will be used by the Midi Event later on.
            {
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(keyboard, 0, 75, 100);
                KD[KBNIndex].Key = keyboard;
                Console.WriteLine("Mapping " + keyboard + " to " + KBNIndex);
                KBNIndex++;
                Thread.Sleep(10);
            }
            #endregion
            return KD;
        }
    }

    public class KeyData
    {
        public keyboardNames Key{ get; set; }
        public bool isCaps { get; set; }
    }
}