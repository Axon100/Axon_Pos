using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Axon.Application.Interfaces.Services;

namespace Axon.Infrastructure.Services
{
    public class LicenseService : ILicenseService
    {
        private const ulong SecretOffset = 623084;

        private string LicenseFilePath
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = Path.Combine(appData, "AxonPOS");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "axon.lic");
            }
        }

        public string GetHardwareId()
        {
            string rawSerial = GetRawMotherboardSerial();
            if (string.IsNullOrWhiteSpace(rawSerial) || rawSerial.Equals("Default string", StringComparison.OrdinalIgnoreCase))
            {
                rawSerial = GetRawCpuSerial();
            }

            if (string.IsNullOrWhiteSpace(rawSerial))
            {
                rawSerial = Environment.MachineName + "_AXON_POS_HW";
            }

            // Convert string to a clean positive numeric code
            ulong numericHash = CalculateNumericHash(rawSerial);
            return numericHash.ToString();
        }

        public bool IsLicenseValid()
        {
            try
            {
                if (!File.Exists(LicenseFilePath))
                {
                    return false;
                }

                var content = File.ReadAllText(LicenseFilePath).Trim();
                if (string.IsNullOrWhiteSpace(content))
                {
                    return false;
                }

                // Check saved license key
                string currentHwId = GetHardwareId();
                if (ulong.TryParse(currentHwId, out ulong hwCode) && ulong.TryParse(content, out ulong savedKey))
                {
                    return checked(savedKey - SecretOffset) == hwCode;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateAndActivate(string licenseKey, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                errorMessage = "يرجى إدخال مفتاح التفعيل.";
                return false;
            }

            string cleanKey = licenseKey.Trim().Replace(" ", "").Replace("-", "");
            if (!ulong.TryParse(cleanKey, out ulong userKey))
            {
                errorMessage = "مفتاح التفعيل غير صحيح (يجب أن يتكون من أرقام فقط).";
                return false;
            }

            string currentHwId = GetHardwareId();
            if (!ulong.TryParse(currentHwId, out ulong hwCode))
            {
                errorMessage = "فشل في قراءة كود البوردة والجهاز.";
                return false;
            }

            // Formula: Key = HardwareCode + 623084
            ulong expectedKey = checked(hwCode + SecretOffset);
            if (userKey != expectedKey)
            {
                errorMessage = "مفتاح التفعيل غير صحيح أو لا يطابق كود هذا الجهاز!";
                return false;
            }

            // Save valid license file
            try
            {
                File.WriteAllText(LicenseFilePath, userKey.ToString());
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"حدث خطأ أثناء حفظ التفعيل: {ex.Message}";
                return false;
            }
        }

        private string GetRawMotherboardSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var serial = obj["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(serial))
                    {
                        return serial;
                    }
                }
            }
            catch
            {
                // Fallback on permission error
            }
            return string.Empty;
        }

        private string GetRawCpuSerial()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var id = obj["ProcessorId"]?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        return id;
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return string.Empty;
        }

        private ulong CalculateNumericHash(string input)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            ulong rawValue = BitConverter.ToUInt64(hash, 0);
            // Formatted to 8-digit clean number
            return (rawValue % 90000000UL) + 10000000UL;
        }
    }
}
