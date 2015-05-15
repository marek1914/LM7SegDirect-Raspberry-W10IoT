using System;
using Windows.Devices.Gpio;
using Windows.System.Threading;

namespace LM7Seg
{
    public sealed class LM7SegDirect
    {
        private bool _isAnode;
        private byte qtyDigits = 1;
        private ThreadPoolTimer timer = null;
        private GpioPin[] pins = new GpioPin[8];
        private GpioPin[] digits = new GpioPin[4];
        private GpioController gpio;

        //--------
        // Define each valid number configuration to show in the display
        private byte[,] seven_seg_digits = new byte[10, 7]
        {
                { 0,0,0,0,0,0,1 },  // = 0
                { 1,0,0,1,1,1,1 },  // = 1
                { 0,0,1,0,0,1,0 },  // = 2
                { 0,0,0,0,1,1,0 },  // = 3
                { 1,0,0,1,1,0,0 },  // = 4
                { 0,1,0,0,1,0,0 },  // = 5
                { 0,1,0,0,0,0,0 },  // = 6
                { 0,0,0,1,1,1,1 },  // = 7
                { 0,0,0,0,0,0,0 },  // = 8
                { 0,0,0,1,1,0,0 }   // = 9
        };

        public int CurrentValue { get; set; }

        public LM7SegDirect(byte segA, byte segB, byte segC, byte segD, byte segE, byte segF, byte segG, byte segDotn, bool isAnode)
        {
            byte[] pin_order = new byte[8];

            _isAnode = isAnode;

            pin_order[0] = segA;
            pin_order[1] = segB;
            pin_order[2] = segC;
            pin_order[3] = segD;
            pin_order[4] = segE;
            pin_order[5] = segF;
            pin_order[6] = segG;
            pin_order[7] = segDotn;

            gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                //@@
            }

            //----------------
            // Open each pin and turn the light of for it
            for (int i = 0; i < 8; i++)
            {
                pins[i] = gpio.OpenPin(pin_order[i]);
                pins[i].Write(GpioPinValue.High);
                pins[i].SetDriveMode(GpioPinDriveMode.Output);
            }

            //----------
            // Create a timer that redraw the currentvalue each second
            // OBS: If you want to change numbers faster than that, just fix the timespan for this timer
            timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(1));
        }

        //-----------------------------
        // defineDigits
        // Define how many digits the 7-segment have and what are the pin numbers
        public void defineDigits(byte digitsQty, byte dig1, byte dig2, byte dig3, byte dig4)
        {
            byte[] digit_order = new byte[4];

            qtyDigits = digitsQty;

            digit_order[0] = dig1;
            digit_order[1] = dig2;
            digit_order[2] = dig3;
            digit_order[3] = dig4;

            //----------------
            // Open each digit and turn the light of for it
            for (int i = 0; i < digitsQty; i++)
            {
                digits[i] = gpio.OpenPin(digit_order[i]);
                digits[i].Write(GpioPinValue.High);
                digits[i].SetDriveMode(GpioPinDriveMode.Output);
            }
        }

        //---------------------------------------------------------------------
        // Write one number to a specific digit in the 7-Segment led
        public void digitWrite(byte digit, int number)
        {
            // If the desired digit does not exist, return
            if (digit > qtyDigits)
                return;

            // Activate the digit
            pickDigit(digit);

            // Activate each led in the digit
            int pinseq = 0;
            for (byte segCount = 0; segCount < 7; ++segCount)
            {
                byte finalValue = seven_seg_digits[number, segCount];
                if (!_isAnode)
                    finalValue = finalValue == (byte)1 ? (byte)0 : (byte)1;

                pins[pinseq].Write(finalValue == 0 ? GpioPinValue.Low : GpioPinValue.High);
                pinseq++;
            }

            // Here I am always turning off the DOT, because I dont want it
            pins[pinseq].Write(_isAnode ? GpioPinValue.High : GpioPinValue.Low);
        }

        //---------------------------------------------------------------------
        // Dump the CurrentValue to the display.
        // Up to 4 digits
        public void valueWrite(int number)
        {
            CurrentValue = number;

            if (timer == null)
                timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(500));
        }

        //--------------
        // Each timer interval redraw the number in the display
        private void Timer_Tick(ThreadPoolTimer timer)
        {
            if (CurrentValue < 10)
            {
                digitWrite(4, 0);
                digitWrite(3, 0);
                digitWrite(2, 0);
                digitWrite(1, CurrentValue);
            }
            else if (CurrentValue < 100)
            {
                digitWrite(4, 0);
                digitWrite(3, 0);
                digitWrite(2, CurrentValue / 10);
                digitWrite(1, CurrentValue % 10);
            }
            else if (CurrentValue < 1000)
            {
                digitWrite(4, 0);
                digitWrite(3, CurrentValue / 100);
                digitWrite(2, (CurrentValue % 100) / 10);
                digitWrite(1, CurrentValue % 10);
            }
            else
            {
                digitWrite(4, CurrentValue / 1000);
                digitWrite(3, (CurrentValue % 1000) / 100);
                digitWrite(2, (CurrentValue % 100) / 10);
                digitWrite(1, CurrentValue % 10);
            }

        }

        //---------------------------------------------------------------------
        // Activate a specific digit in the 7-Segment.
        private void pickDigit(int x)
        {
            // If only one digit (7-Segment 1 Digit) then no need to continue here
            if (qtyDigits == 1)
                return;

            // Turn off ALL digits
            for (int i = 0; i < qtyDigits; i++)
            {
                digits[i].Write(_isAnode ? GpioPinValue.Low : GpioPinValue.High);
            }

            // Turn ON only the desired digit
            digits[x - 1].Write(_isAnode ? GpioPinValue.High : GpioPinValue.Low);
        }
    }
}
