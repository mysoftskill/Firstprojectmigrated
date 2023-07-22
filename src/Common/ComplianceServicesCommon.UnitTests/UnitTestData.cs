// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common.UnitTests
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    public static class UnitTestData
    {
        // This unit test cert was generated via the PowerShell script 'New-SelfSignedCertificate' and is only intended for use by unit tests.
        public const string UnitTestCert = "MIIKRQIBAzCCCgEGCSqGSIb3DQEHAaCCCfIEggnuMIIJ6jCCBisGCSqGSIb3DQEHAaCCBhwEggYYMIIGFDCCBhAGCyqGSIb3DQEMCgECoIIE/jCCBPowHAYK" +
                                           "KoZIhvcNAQwBAzAOBAgPP8azijIglAICB9AEggTYnasEbznQV3rSZuJv8SHQESIYHddyXE68gxlJMA5LWGexkkVqcVTdk6P37BWAg/K+/fUEhK33YF0T5ANe" +
                                           "A8wcZPWLv5sFc6LUeA9hB2OPuMnTTXDdEj6c70MxzVcIoeDJn6qjYc6ZsaKNBS8Few8NWDoNSMfgCY388xd27WNjp2krwppsN37DYqpuLumcDYT27j70dmxP" +
                                           "PJSk0XiyHanTVdlQch05SmSXS6wMMeALhqnSd/rLxpJglYlSJkyApR6vDdTP9PampwEhDnTfleCx1EnYnYGNp4Kz83203CeNA2pgSm35PMqZ30zoU4gRj4DO" +
                                           "LbtF9vGbp/492Xcd3cuuNxLedEyWc5zPzKhZSQ+68DwgF6up5OO8VYqUMSgn5VR/F2LbJA8XuYgFa18ewF/XNYAJG6KS4kS6JAO3LDBav3Qq/hKp9R+O1vgc" +
                                           "THjxiPEPeeRGEq66/OuVbHgm8O9Ie1Sd3xon1r0+W+/ZsJfSvcu7QYjkz+vIQz2oWYLcjEwtyWkltQgXzPGpqSunasUEVi+g5KU2Qv+CtZNCySGM8OoQ+Rq3" +
                                           "Qqid/VJdXICLMx4FROTe5XvAshOL21IC9Wh6GFZkZEH7rAFhh3SXsFA4z9U+Mp08lO1SeZAauV2pdc/oZUNGMsiTesMZZZDrAohBKczjwvU85kkvkGXOukJA" +
                                           "ycePrgWAZHWUkOMG3oYJ7TaX0KQkG7tSogUJUI0tELB0GGYQFFr8Arl9AvXxLlb7TooR7hysoMQitw8Boe7LmFM1IPbPGXdT/VqhIHPbbqmEHkIo9BUQnoI4" +
                                           "0OsiG6ji0xiFcZXTBzzZggXdk1QK71XSKcqG3qd8kRLz/MB3ufuFXS0yiCLNyEx0JOL0dkiS6BR6L5+uXX2IV5AkrCwGp6/vxyyP1srDaz9VaJZ5yEt7oVJI" +
                                           "jcsGVnqWcsGaDfhkf/fnA7hNLnqThNxslwhj+uA5vfF/dlAi8y20UvFKE29eMKx3nfxQK2tVFwjUyzAM0NKenApj+gvhIxn8gjKqwPRy4353Ym2N2qVF5/fa" +
                                           "yL9GEy6DqV3zYO9KZsSzhq+qbGrMRDXinnIegMECMiLnvnKGlnZahROAh3BzSwQz/CGEu06Y8yv4eoJGyH6eCjecHmBdUA24WOEQvCedQvNo6ILX9q4YUh3a" +
                                           "xfm6M7S5krPtpOUhMjtYTu7j+VRWbxgzFE6ChvZLFPJ1+L6ckWwKDSz/Y07JUjkzTt60iFCQHUUFDsZEYQaxomKL39vaEu//x4ctM95DACez+K8IggXq3hT3" +
                                           "uXpGPCNrVOGEpwxagtb5EeZXbiFaGAF9zvuLRcX9G+Tj0gy8UjwrvTwAPxT59sDJYmN4xnk+QiepTyMty22kh9Sm+026NnxSo8SDeXmFGLQ81DlHIdgEWyI+" +
                                           "I9yjZtR2N04/1Og7ClQ00TNw4X59/1G9WUeTWsbQNg33jR1H+VR7uZbIG91gDqMMpNQoEh5FI9289CFGKhwJFMRNgXOYJ+sL9p1PkFw2K2kcXYqmC1XmByxS" +
                                           "KUKCYpuuyE5Fl5s1U+1d+mFPwtWIM1RAy1Wl9cQQQPiEmmU/z2pFWbogNTU25koYk0cIyrv2OQC/iqaMtd/zTTzPpYTkBgC5YzajE+fZe+VNq3UbbRalS8bQ" +
                                           "tjsOX7Nr+zMWFDGB/jANBgkrBgEEAYI3EQIxADATBgkqhkiG9w0BCRUxBgQEAQAAADBdBgkqhkiG9w0BCRQxUB5OAHQAZQAtADAAMQA3AGQAMQAwAGMAZgAt" +
                                           "ADYAOQA5AGEALQA0ADcANQBmAC0AOAA5ADkAOAAtADgAOABjADEAZABkADIANQBlADAAMwAzMHkGCSsGAQQBgjcRATFsHmoATQBpAGMAcgBvAHMAbwBmAHQA" +
                                           "IABFAG4AaABhAG4AYwBlAGQAIABSAFMAQQAgAGEAbgBkACAAQQBFAFMAIABDAHIAeQBwAHQAbwBnAHIAYQBwAGgAaQBjACAAUAByAG8AdgBpAGQAZQByMIID" +
                                           "twYJKoZIhvcNAQcGoIIDqDCCA6QCAQAwggOdBgkqhkiG9w0BBwEwHAYKKoZIhvcNAQwBAzAOBAidFFcfkFg/OwICB9CAggNwyu14/BfV0eQ+1lR+SyytRo4L" +
                                           "kjBzWQNHCvhFdP+t2K44B9wwIOVtEokY8ZFLHh5XeskrSXyXFf5MGe4KVL0Jfkg+OOtwSDbwdiPCi/LDnPKSOazorvUeYJ9BRvAQ8ihJkVBXr2RStNERd5Mo" +
                                           "nMCgB9q+16sZzpRf5X6+hlSSUoKx7b04Vf4HkTZN7KwGHA5hP5PFpo0sR/ZcMXeId+rBqpG4QGfoMLMlPy0+Sw9MyKIfyRxfGXBYD+sigX+Tu20qTqw+H+5N" +
                                           "16FbUqB/jlKnYGw4jqPnz7EoCQ1c3eunnfcZQ6mvLVni35GrXXbBCdNE8KS6y6ZpR8fagacQfIjuT1YJhZSTIdUDx3uASCP5A/TERDakrUdPltHNhGrGHekQ" +
                                           "FZ9Ofk+ZYmd37Wn+oUc9LeOxUqys5g15hvVo6QLGAeHv1bcfVo5XRl3I9bcGVFq3xLMxUJbDS1+d8JrnaM9nveIizoDqP5E2ukHgLjNS7fM2Px7XS4D8+V0H" +
                                           "yc+7PiGbZXPMX92Y+fLF7RQ5MsYkfNNUUYTTscBqF/OsDgNRr7gunHmv6QNPC5dtkii5+MZ2YefahLSDuSsF81Gsd7ASoBNjeeW8d3MnZ2QSuc06qFEfu4RR" +
                                           "j/JVwhnzhzwmxfuq/qLVfVI3wbaule2mDXLMh4iJHaFDcvYXUw7QDfGWviQzxpEv7Ccd3necYsV5f/FTngSXQ9QlsDjHswrZdqAEcAIrx3ai83UBRk2Uxqcs" +
                                           "zoZeu9rtxPAIW1fduphqpxaorSDkG45nok1Pxo81MmD2hHOmD9R/9QikzbltTKS8tGKB80HhsWP5CZMRTphsoC8n/DXgHHJEzymtGGUMEZmFLJin+rYRgO0b" +
                                           "awYnS2FaWYgtjiihqmVNzPvYUKeBwNg+B8yD8r9mbfMtFq4n72bzEI7D5oLhbmHfc1FKzVxCIRwKkNRbqase4PTtgXHK2c/qVfHljUwk/ms6xjITYj/pRxmz" +
                                           "bCPWWMbhk/dnH0rSzIqyO8ZAbpG3W//0YsJU1Sr0QcvhMYPCVdsenNbtQ9+DB4bdvCxsd9nZAidxwjG3GYoz0cyN40o9Odumc2E0Cy7YygAolFsHnZoUzdm9" +
                                           "+VdzyUCpxKoEmyQZqVzFNVouVO2/29nP7J4mOp9wUxhfgnVB0D0b6eytvPx7WbrOWjIqCDA7MB8wBwYFKw4DAhoEFLfQ3ddxvaJ0gszQO3mNEeBhwj8nBBQ2" +
                                           "Dxx0pmGVCSl7MeXzubZaZFtiJwICB9A=";

        /// <summary>
        ///     This unit test cert was generated via PowerShell script: New-SelfSignedCertificate -Subject "unittest" -NotBefore "11/25/2019" -NotAfter "11/25/2100"
        /// </summary>
        public const string UnitTestCert2 = "MIIDAjCCAeqgAwIBAgIQLp+KgNennINH8uJfje7bfzANBgkqhkiG9w0BAQsFADATMREwDwYDVQQDDAh1bml0dGVzdDAgFw0xOTExMjUwODAwMDBaGA8yMTAw" +
                                            "MTEyNTA4MDAwMFowEzERMA8GA1UEAwwIdW5pdHRlc3QwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQDDMjxpVuXvlVLhkQuWtqDi7V7lA+O5N2kA" +
                                            "FDTtodKwciyxZY3LOeYmB7amX1bIt227aRj+kpMkmbRKcBQwXN1BC3sFMW+lloaTPhi2Y+tjKMvA+iQNWUpdvtE36U3AqdwBgmx/QyEfW45yMKfscrq/musM" +
                                            "xDoo591rngR8U8LcrWpWTNT0AfUbDIVeX7SjtsJNH+E3+NIjw7RB8B0r8hU2JBBYjQylfD45AMaS0RYSKEevAD1cGj15LMQodlBHl6t0tml14O1CXBnyqhwA" +
                                            "GxsPjeeNNCWdTcm8ZCTm9ld5tvh6LCWPuZPnApWuygKIld9ob+/12karr925/zTBQv5ZAgMBAAGjUDBOMA4GA1UdDwEB/wQEAwIFoDAdBgNVHSUEFjAUBggr" +
                                            "BgEFBQcDAgYIKwYBBQUHAwEwHQYDVR0OBBYEFOGDrMOchO+kKvedhYT92MXUycmXMA0GCSqGSIb3DQEBCwUAA4IBAQC0NlQ48ATXO/ZvQhcA3+iia5crptj8" +
                                            "GLuqZxH6oHj+ih/gKobbN4gpxl9OPp2iekgdIbtegZsnYfQm+PTqAI2/i6Pzf42VCVCi/7J9Mbxg9YFuSR8QcEtqU1AL+0TqW+XX8njgG9R49cA9+waqiKH1" +
                                            "tvZIDEvDlutEs6+7BqD8++CAEcPNEZqWyOP9dxtlCrTF1iPWme3PFi8I3LStLIKCARlj1BePCdQo4Hvq8tWoS7ZUe5NfZdfTG040BJRufE2kcMwmhehn+cMW" +
                                            "uuFYSPZb4hE9B1LbkumpVx0qX6WYBF6HnlxWefmTrQAbc84C3c2kVpwKVA/mie1xHb0Y2/+Y";

        /// <summary>
        ///     This unit test cert was generated via PowerShell script: 
        ///     $cert = New-SelfSignedCertificate -Subject "aad.unittest" -NotBefore "11/25/2019" -NotAfter "11/25/2101"
        ///     [System.Convert]::ToBase64String($cert.RawData)
        /// </summary>
        public const string UnitTestCert3 = "MIIDCjCCAfKgAwIBAgIQPvTQizqe3pBH1G0pDJDvNTANBgkqhkiG9w0BAQsFADAXMRUwEwYDVQQDDAxhYWQudW5pdHRlc3QwIBcNMTkxMTI1MDcwMDAwWhgP" + 
                                            "MjEwMTExMjUwNzAwMDBaMBcxFTATBgNVBAMMDGFhZC51bml0dGVzdDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAPW17o8gYQg03kaj/owVMbf/" +
                                            "X1BxnVDfiRlACdigypUq9rahO6EGgkawB3UwpJIzB4LPb0zTO/kRnhfFqWibVsrN+xIPTHkTafZcRj1OjG9+nR1FSdawpOo5Wpr5zxXpI/J1yOoKob7ueDHx" +
                                            "QzdBxFJcaTO1koIsNiNxZC1FqUjLpcAhM4A2Rn8r0yle+dvufRDsS7EvBUpGEjiibhySfbMuJ721KCleQ0lM0Oym/tknJpAK2gbN6CgMnqsKk6rijZd7srBN" + 
                                            "uF7hEPEbo7mAa7OK/ig5ldZuDMjhYuASXbSIpQycHE6ERDy90YpO1pELT6suJkgkWjjGbFQW59zVEn0CAwEAAaNQME4wDgYDVR0PAQH/BAQDAgWgMB0GA1Ud" + 
                                            "JQQWMBQGCCsGAQUFBwMCBggrBgEFBQcDATAdBgNVHQ4EFgQU40nouVOkkriAwACHz1nAnDRFBycwDQYJKoZIhvcNAQELBQADggEBAFf5jQlkyKMszCfnWgiy" +
                                            "60JfXN9XYfklkntBjn4ux6u9zgaKlfbDa1EBcCVBLKut+l0fC+GL8ZzgtTHJ45Ew2kJzYJnkuouiWzyMCn26Lqcae95ZVy4uqDZlN6JTU/UDVZluRL3YjITr" +
                                            "B00p0TQP/X62ogXpex78poaMt3ZnbC6ZxuegT/BKfrFRetA1e84oTMR6f5hcepj5Gh/KtSEClvjxVtMgJSSgrkTRNzbR52j8srAhzajwS5SbZD7o5u3vg49O" +
                                            "vRk8uuEpeC9tY/ARaUD8xhHFSsQRwFms02cL/FgtBoBsZHSVatOG5rc1YylujTHlvoXwbPlv7C/h7/3CFhU=";

        public static readonly X509Certificate2 UnitTestCertificate = new X509Certificate2(Convert.FromBase64String(UnitTestCert));

        public static readonly X509Certificate2 UnitTestCertificate2 = new X509Certificate2(Convert.FromBase64String(UnitTestCert2));

        public static readonly X509Certificate2 UnitTestCertificate3 = new X509Certificate2(Convert.FromBase64String(UnitTestCert3));
    }
}
