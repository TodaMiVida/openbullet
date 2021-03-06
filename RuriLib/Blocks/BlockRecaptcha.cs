﻿using RuriLib.CaptchaServices;
using RuriLib.LS;
using RuriLib.Models;
using System;
using System.Windows.Media;

namespace RuriLib
{
    /// <summary>
    /// A block that solves a reCaptcha challenge.
    /// </summary>
    public class BlockRecaptcha : BlockCaptcha
    {
        private string variableName = "";
        /// <summary>The name of the output variable where the challenge solution will be stored.</summary>
        public string VariableName { get { return variableName; } set { variableName = value; OnPropertyChanged(); } }

        private string url = "https://google.com";
        /// <summary>The URL where the reCaptcha challenge appears.</summary>
        public string Url { get { return url; } set { url = value; OnPropertyChanged(); } }

        private string siteKey = "";
        /// <summary>The Google SiteKey found in the page's source code.</summary>
        public string SiteKey { get { return siteKey; } set { siteKey = value; OnPropertyChanged(); } }

        /// <summary>
        /// Creates a reCaptcha block.
        /// </summary>
        public BlockRecaptcha()
        {
            Label = "RECAPTCHA";
        }

        /// <inheritdoc />
        public override BlockBase FromLS(string line)
        {
            // Trim the line
            var input = line.Trim();

            // Parse the label
            if (input.StartsWith("#"))
                Label = LineParser.ParseLabel(ref input);

            /*
             * Syntax:
             * RECAPTCHA "URL" "SITEKEY"->VAR "RECAP"
             * */

            Url = LineParser.ParseLiteral(ref input, "URL");
            SiteKey = LineParser.ParseLiteral(ref input, "SITEKEY");

            if (LineParser.ParseToken(ref input, TokenType.Arrow, false) == "")
                return this;

            LineParser.EnsureIdentifier(ref input, "VAR");

            // Parse the variable name
            VariableName = LineParser.ParseLiteral(ref input, "VARIABLE NAME");

            return this;
        }

        /// <inheritdoc />
        public override string ToLS(bool indent = true)
        {
            var writer = new BlockWriter(GetType(), indent, Disabled);
            writer
                .Label(Label)
                .Token("RECAPTCHA")
                .Literal(Url)
                .Literal(SiteKey)
                .Arrow()
                .Token("VAR")
                .Literal(VariableName);
            return writer.ToString();
        }

        /// <inheritdoc />
        public override void Process(BotData data)
        {
            if(!data.GlobalSettings.Captchas.BypassBalanceCheck)
                base.Process(data);

            data.Log(new LogEntry("Solving reCaptcha...", Colors.White));

            string recapResponse = "";
            CaptchaServices.CaptchaService service = null;

            switch (data.GlobalSettings.Captchas.CurrentService)
            {
                case CaptchaService.ImageTypers:
                    service = new ImageTyperz(data.GlobalSettings.Captchas.ImageTypToken, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.AntiCaptcha:
                    service = new AntiCaptcha(data.GlobalSettings.Captchas.AntiCapToken, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.DBC:
                    service = new DeathByCaptcha(data.GlobalSettings.Captchas.DBCUser, data.GlobalSettings.Captchas.DBCPass, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.TwoCaptcha:
                    service = new TwoCaptcha(data.GlobalSettings.Captchas.TwoCapToken, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.RuCaptcha:
                    service = new RuCaptcha(data.GlobalSettings.Captchas.RuCapToken, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.DeCaptcher:
                    service = new DeCaptcher(data.GlobalSettings.Captchas.DCUser, data.GlobalSettings.Captchas.DCPass, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.AZCaptcha:
                    service = new AZCaptcha(data.GlobalSettings.Captchas.AZCapToken, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.SolveRecaptcha:
                    service = new SolveReCaptcha(data.GlobalSettings.Captchas.SRUserId, data.GlobalSettings.Captchas.SRToken, data.GlobalSettings.Captchas.Timeout);
                    break;

                case CaptchaService.CaptchasIO:
                    service = new CaptchasIO(data.GlobalSettings.Captchas.CIOToken, data.GlobalSettings.Captchas.Timeout);
                    break;

                default:
                    throw new Exception("This service cannot solve reCaptchas!");
            }
            recapResponse = service.SolveRecaptcha(siteKey, ReplaceValues(url, data));

            data.Log(recapResponse == "" ? new LogEntry("Couldn't get a reCaptcha response from the service", Colors.Tomato) : new LogEntry("Succesfully got the response: " + recapResponse, Colors.GreenYellow));
            if (VariableName != "")
            {
                data.Log(new LogEntry("Response stored in variable: " + variableName, Colors.White));
                data.Variables.Set(new CVar(variableName, recapResponse));
            }
        }
    }
}
