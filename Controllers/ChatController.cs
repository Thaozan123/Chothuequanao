using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChoThueQuanAo.Controllers
{
    public class ChatController : Controller
    {
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Ask(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = true, answer = "Chào bạn! Hãy nhập câu hỏi về thuê đồ, size, giá, màu sắc hoặc cọc nhé." });
            }

            var normalized = message.Trim().ToLowerInvariant();
            var answer = GenerateAnswer(normalized, message);
            return Json(new { success = true, answer });
        }

        private string GenerateAnswer(string normalized, string originalMessage)
        {
            // ===== QUẦN - HỎI VỀ SIZE QUẦN =====
            if (normalized.Contains("quần") || normalized.Contains("quan") || normalized.Contains("quann") ||
                normalized.Contains("pant") || normalized.Contains("jean") || normalized.Contains("cho nam") || normalized.Contains("cho nữ"))
            {
                return GetPantsAdvice(normalized, originalMessage);
            }

            // ===== ÁO SƠ MI - HỎI VỀ SHIRT/SOA MI =====
            if (normalized.Contains("sơ mi") || normalized.Contains("shirt") || normalized.Contains("áo xuyên") || normalized.Contains("áo"))
            {
                return GetShirtAdvice(normalized, originalMessage);
            }

            // ===== SIZE CHUNG (ÁO) - TRỊ VẤN SIZE CHI TIẾT =====
            if (normalized.Contains("size") || normalized.Contains("cỡ") || normalized.Contains("kích cỡ") || 
                normalized.Contains("mặc") || normalized.Contains("vóc") || normalized.Contains("áo"))
            {
                return GetDetailedSizeAdvice(normalized, originalMessage);
            }

            if (normalized.Contains("giá") || normalized.Contains("thuê") || normalized.Contains("tiền"))
            {
                return "Giá thuê sẽ hiển thị trên chi tiết sản phẩm. Nếu bạn thuê nhiều ngày, hãy kiểm tra chương trình khuyến mãi để được giảm giá.";
            }
            if (normalized.Contains("màu") || normalized.Contains("phối") || normalized.Contains("style"))
            {
                return "Màu sắc sản phẩm nên phối cùng trang phục hoặc phụ kiện có tông hài hòa. Màu tối phù hợp sự kiện trang trọng, màu sáng phù hợp tiệc tân thời.";
            }
            if (normalized.Contains("cọc") || normalized.Contains("deposit") || normalized.Contains("đặt cọc"))
            {
                return "Tiền cọc được thu trước để đảm bảo an toàn sản phẩm. Hãy giữ đồ nguyên vẹn và trả đúng hạn để được hoàn cọc nhanh chóng.";
            }
            if (normalized.Contains("hết hàng") || normalized.Contains("còn hàng") || normalized.Contains("tồn kho"))
            {
                return "Bạn có thể kiểm tra số lượng còn lại trên trang sản phẩm. Nếu còn ít, hãy đặt sớm để giữ sản phẩm.";
            }
            if (normalized.Contains("hợp") || normalized.Contains("sự kiện") || normalized.Contains("đám cưới") || normalized.Contains("tiệc"))
            {
                return "Các sản phẩm áo dài thường rất phù hợp cho các sự kiện trang trọng, chụp ảnh hay lễ hội. Áo sơ mi phù hợp sự kiện công sở, hẹn hò, đi chơi. Quần phù hợp nhiều dịp khác nhau.";
            }

            return "Mình đã nhận câu hỏi rồi. Bạn có thể hỏi cụ thể hơn về size áo/quần, giá thuê, màu sắc, cọc hoặc sự kiện phù hợp. Nếu không, mình sẽ gợi ý thêm nhé.";
        }

        private string GetPantsAdvice(string normalized, string originalMessage)
        {
            int? weight = null;

            // Parse cân nặng
            if (normalized.Contains("kg"))
            {
                var numbers = Regex.Matches(originalMessage, @"\d+");
                if (numbers.Count > 0 && int.TryParse(numbers[0].Value, out int w))
                {
                    weight = w;
                }
            }

            // Bảng size quần NỮ
            var pantsWomen = new[]
            {
                new { Size = "S", Height = "148-152cm", Weight = "38-42kg", WaistGirth = 66, Length = 90 },
                new { Size = "M", Height = "153-157cm", Weight = "43-47kg", WaistGirth = 70, Length = 92 },
                new { Size = "L", Height = "158-162cm", Weight = "48-52kg", WaistGirth = 74, Length = 94 },
                new { Size = "XL", Height = "163-167cm", Weight = "53-57kg", WaistGirth = 78, Length = 96 },
                new { Size = "XXL", Height = "168-172cm", Weight = "58-62kg", WaistGirth = 80, Length = 98 }
            };

            // Bảng size quần NAM
            var pantsMen = new[]
            {
                new { Size = "S (28)", Height = "160-168cm", Weight = "45-58kg", Chest = 85 },
                new { Size = "M (29)", Height = "168-173cm", Weight = "58-68kg", Chest = 87 },
                new { Size = "L (30)", Height = "173-177cm", Weight = "64-74kg", Chest = 89 },
                new { Size = "XL (31)", Height = "177-182cm", Weight = "74-82kg", Chest = 91 },
                new { Size = "2XL (32)", Height = "182-187cm", Weight = "80-90kg", Chest = 93 },
                new { Size = "3XL (33)", Height = "187-191cm", Weight = "88-98kg", Chest = 95 },
                new { Size = "4XL (34)", Height = "191+cm", Weight = "95+kg", Chest = 97 }
            };

            // Kiểm tra giới tính
            bool isWomen = normalized.Contains("nữ");

            // Hỏi về quần nữ
            if (isWomen)
            {
                if (weight.HasValue)
                {
                    var matchedSize = pantsWomen.FirstOrDefault(p =>
                    {
                        var parts = p.Weight.Split('-');
                        if (int.TryParse(parts[0], out int minW) && int.TryParse(parts[1].Replace("kg", ""), out int maxW))
                        {
                            return weight >= minW && weight <= maxW;
                        }
                        return false;
                    });

                    if (matchedSize != null)
                    {
                        return $"✅ **Nữ - Với cân nặng {weight}kg, bạn nên chọn quần size {matchedSize.Size}**!\n\n" +
                               $"📊 Chi tiết quần nữ size {matchedSize.Size}:\n" +
                               $"   • Chiều cao: {matchedSize.Height}\n" +
                               $"   • Cân nặng: {matchedSize.Weight}\n" +
                               $"   • Vòng eo: {matchedSize.WaistGirth}cm\n" +
                               $"   • Dài quần: {matchedSize.Length}cm\n\n" +
                               $"Lựa chọn hoàn hảo cho bạn!";
                    }
                }

                return $"📊 **Bảng size quần nữ:**\n\n" +
                       $"   **S:** 148-152cm | 38-42kg | Eo 66cm | Dài 90cm\n" +
                       $"   **M:** 153-157cm | 43-47kg | Eo 70cm | Dài 92cm\n" +
                       $"   **L:** 158-162cm | 48-52kg | Eo 74cm | Dài 94cm ← Phổ biến\n" +
                       $"   **XL:** 163-167cm | 53-57kg | Eo 78cm | Dài 96cm\n" +
                       $"   **XXL:** 168-172cm | 58-62kg | Eo 80cm | Dài 98cm\n\n" +
                       $"Cho mình biết cân nặng hoặc chiều cao để tư vấn chính xác!";
            }

            // Hỏi về quần nam (default nếu không chỉ định)
            if (weight.HasValue)
            {
                var matchedSize = pantsMen.FirstOrDefault(p =>
                {
                    var parts = p.Weight.Split('-');
                    int minW = int.Parse(parts[0]);
                    int maxW = parts[1].Contains("+") ? 999 : int.Parse(parts[1].Replace("kg", ""));
                    return weight >= minW && weight <= maxW;
                });

                if (matchedSize != null)
                {
                    return $"✅ **Nam - Với cân nặng {weight}kg, bạn nên chọn quần size {matchedSize.Size}**!\n\n" +
                           $"📊 Chi tiết quần nam {matchedSize.Size}:\n" +
                           $"   • Chiều cao: {matchedSize.Height}\n" +
                           $"   • Cân nặng phù hợp: {matchedSize.Weight}\n" +
                           $"   • Ngực: {matchedSize.Chest}cm\n\n" +
                           $"Size này sẽ rất thoải mái!";
                }
            }

            return $"📊 **Bảng size quần nam:**\n\n" +
                   $"   **S (28):** 160-168cm | 45-58kg | Ngực 85cm\n" +
                   $"   **M (29):** 168-173cm | 58-68kg | Ngực 87cm ← Phổ biến\n" +
                   $"   **L (30):** 173-177cm | 64-74kg | Ngực 89cm\n" +
                   $"   **XL (31):** 177-182cm | 74-82kg | Ngực 91cm\n" +
                   $"   **2XL (32):** 182-187cm | 80-90kg | Ngực 93cm\n" +
                   $"   **3XL (33):** 187-191cm | 88-98kg | Ngực 95cm\n\n" +
                   $"Cho mình biết cân nặng để tư vấn!";
        }

        private string GetShirtAdvice(string normalized, string originalMessage)
        {
            int? weight = null;

            // Parse cân nặng
            var numbers = Regex.Matches(originalMessage, @"\d+");
            if (numbers.Count > 0 && int.TryParse(numbers[0].Value, out int w))
            {
                weight = w;
            }

            // Bảng size áo sơ mi NAM
            var shirtMen = new[]
            {
                new { Size = "S", Height = "160-165cm", Chest = "164-169cm", Weight = "55-60kg" },
                new { Size = "M", Height = "170-174cm", Chest = "170-174cm", Weight = "66-70kg" },
                new { Size = "L", Height = "174-176cm", Chest = "175-178cm", Weight = "70-76kg" },
                new { Size = "XL", Height = "176-178cm", Chest = "178-182cm", Weight = "76-78kg" }
            };

            // Bảng size áo sơ mi NỮ
            var shirtWomen = new[]
            {
                new { Size = "S", Height = "148-153cm", Chest = "153-155cm", Weight = "38-43kg" },
                new { Size = "M", Height = "155-158cm", Chest = "155-158cm", Weight = "43-46kg" },
                new { Size = "L", Height = "158-162cm", Chest = "158-162cm", Weight = "53-57kg" },
                new { Size = "XL", Height = "162-166cm", Chest = "162-166cm", Weight = "57-66kg" }
            };

            // Kiểm tra giới tính
            bool isWomen = normalized.Contains("nữ");

            var shirtChart = isWomen ? shirtWomen : shirtMen;
            var gender = isWomen ? "Nữ" : "Nam";

            if (weight.HasValue)
            {
                var matched = shirtChart.FirstOrDefault(s =>
                {
                    var parts = s.Weight.Split('-');
                    if (int.TryParse(parts[0], out int minW) && int.TryParse(parts[1].Replace("kg", ""), out int maxW))
                    {
                        return weight >= minW && weight <= maxW;
                    }
                    return false;
                });

                if (matched != null)
                {
                    return $"✅ **Áo sơ mi {gender} - Với cân nặng {weight}kg, bạn nên chọn size {matched.Size}**!\n\n" +
                           $"📊 Chi tiết áo sơ mi {gender} size {matched.Size}:\n" +
                           $"   • Chiều cao: {matched.Height}\n" +
                           $"   • Ngực: {matched.Chest}\n" +
                           $"   • Cân nặng: {matched.Weight}\n\n" +
                           $"Size {matched.Size} sẽ rất vừa vặn cho bạn!";
                }
            }

            // Hiển thị bảng
            return $"📊 **Bảng size áo sơ mi {gender}:**\n\n" +
                   (isWomen ? 
                   $"   **S:** 148-153cm | Ngực 153-155cm | 38-43kg\n\n" +
                   $"   **M:** 155-158cm | Ngực 155-158cm | 43-46kg\n\n" +
                   $"   **L:** 158-162cm | Ngực 158-162cm | 53-57kg ← Phổ biến\n\n" +
                   $"   **XL:** 162-166cm | Ngực 162-166cm | 57-66kg\n" 
                   : 
                   $"   **S:** 160-165cm | Ngực 164-169cm | 55-60kg\n\n" +
                   $"   **M:** 170-174cm | Ngực 170-174cm | 66-70kg ← Phổ biến\n\n" +
                   $"   **L:** 174-176cm | Ngực 175-178cm | 70-76kg\n\n" +
                   $"   **XL:** 176-178cm | Ngực 178-182cm | 76-78kg\n") +
                   $"\nCho mình biết cân nặng để tư vấn chính xác!";
        }

        private string GetDetailedSizeAdvice(string normalized, string originalMessage)
        {
            // Parse cân nặng
            int? weight = null;
            if (normalized.Contains("kg"))
            {
                var numbers = Regex.Matches(originalMessage, @"\d+");
                if (numbers.Count > 0 && int.TryParse(numbers[0].Value, out int w))
                {
                    weight = w;
                }
            }

            // Bảng size Châu Á
            var sizeData = new[]
            {
                new { Size = "S", MinWeight = 45, MaxWeight = 52, Chest = "85-90cm", Measurements = "Vai 41cm, Dài áo 60cm" },
                new { Size = "M", MinWeight = 54, MaxWeight = 61, Chest = "90-95cm", Measurements = "Vai 43cm, Dài áo 65cm" },
                new { Size = "L", MinWeight = 62, MaxWeight = 69, Chest = "95-100cm", Measurements = "Vai 45cm, Dài áo 69cm" },
                new { Size = "XL", MinWeight = 70, MaxWeight = 77, Chest = "100-105cm", Measurements = "Vai 47cm, Dài áo 71cm" },
                new { Size = "2XL", MinWeight = 78, MaxWeight = 85, Chest = "105-110cm", Measurements = "Vai 49cm, Dài áo 73cm" },
                new { Size = "3XL", MinWeight = 86, MaxWeight = 93, Chest = "110-115cm", Measurements = "Vai 50cm, Dài áo 75cm" }
            };

            // ===== NẾU CÓ CÂN NẶNG: TÌM SIZE PHÙHỢP =====
            if (weight.HasValue)
            {
                var matchedSize = sizeData.FirstOrDefault(s => weight >= s.MinWeight && weight <= s.MaxWeight);

                if (matchedSize != null)
                {
                    return $"✅ **Áo - Với cân nặng {weight}kg, bạn nên chọn size {matchedSize.Size}**!\n\n" +
                           $"📏 Chi tiết size {matchedSize.Size}:\n" +
                           $"   • Vòng ngực: {matchedSize.Chest}\n" +
                           $"   • Trọng lượng: {matchedSize.MinWeight}-{matchedSize.MaxWeight}kg\n" +
                           $"   • {matchedSize.Measurements}\n\n" +
                           $"💡 Hãy xem chi tiết sản phẩm để kiểm tra size của nó!";
                }

                if (weight < 45)
                    return $"✅ **Cân nặng {weight}kg - Size S phù hợp nhất**\n\n" +
                           $"📊 Bảng size Châu Á:\n" +
                           $"   • S: 85-90cm ngực, 45-52kg ✓\n" +
                           $"   • M: 90-95cm ngực, 54-61kg\n" +
                           $"   • L: 95-100cm ngực, 62-69kg\n\n" +
                           $"Size S sẽ vừa vặn!";

                if (weight < 65)
                    return $"✅ **Cân nặng {weight}kg - Size M phù hợp nhất**\n\n" +
                           $"📊 Bảng size Châu Á:\n" +
                           $"   • S: 85-90cm ngực, 45-52kg\n" +
                           $"   • M: 90-95cm ngực, 54-61kg ✓\n" +
                           $"   • L: 95-100cm ngực, 62-69kg\n\n" +
                           $"Size M thoải mái!";

                if (weight < 75)
                    return $"✅ **Cân nặng {weight}kg - Size L phù hợp nhất**\n\n" +
                           $"📊 Bảng size Châu Á:\n" +
                           $"   • M: 90-95cm ngực, 54-61kg\n" +
                           $"   • L: 95-100cm ngực, 62-69kg ✓\n" +
                           $"   • XL: 100-105cm ngực, 70-77kg\n\n" +
                           $"Size L cho vóc dáng cao!";

                return $"✅ **Cân nặng {weight}kg - Size XL hoặc lớn hơn**\n\n" +
                       $"📊 Bảng size Châu Á:\n" +
                       $"   • L: 95-100cm ngực, 62-69kg\n" +
                       $"   • XL: 100-105cm ngực, 70-77kg ✓\n\n" +
                       $"Chọn size lớn sẽ thoải mái!";
            }

            if (normalized.Contains("nhỏ") || normalized.Contains("mảnh"))
            {
                return $"👤 **Vóc dáng nhỏ gọn? Size S!**\n\n" +
                       $"📏 Size S: 85-90cm ngực | 45-52kg | Vai 41cm\n\n" +
                       $"Size S sẽ rất vừa vặn!";
            }

            if (normalized.Contains("vừa") || normalized.Contains("bình thường"))
            {
                return $"👤 **Vóc dáng bình thường? Size M!**\n\n" +
                       $"📏 Size M: 90-95cm ngực | 54-61kg | Vai 43cm\n\n" +
                       $"Size M thoải mái!";
            }

            if (normalized.Contains("cao") || normalized.Contains("to"))
            {
                return $"👤 **Vóc dáng cao/to? Size L!**\n\n" +
                       $"📏 Size L: 95-100cm ngực | 62-69kg | Vai 45cm\n\n" +
                       $"Size L cho bạn thoải mái!";
            }

            return $"📊 **Bảng size chi tiết - Châu Á:**\n\n" +
                   $"   **S:** 85-90cm ngực | 45-52kg | Vai 41cm | Dài áo 60cm\n\n" +
                   $"   **M:** 90-95cm ngực | 54-61kg | Vai 43cm | Dài áo 65cm ← Phổ biến\n\n" +
                   $"   **L:** 95-100cm ngực | 62-69kg | Vai 45cm | Dài áo 69cm\n\n" +
                   $"   **XL:** 100-105cm ngực | 70-77kg | Vai 47cm | Dài áo 71cm\n\n" +
                   $"💡 Hãy cho mình biết cân nặng để tư vấn chính xác!";
        }
    }
}
