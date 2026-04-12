using ChoThueQuanAo.Data;
using ChoThueQuanAo.Models;
using ChoThueQuanAo.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace ChoThueQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================================
        // DÀNH CHO TẤT CẢ MỌI NGƯỜI (Xem hàng & Tìm kiếm)
        // ==========================================================

        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên thanh tìm kiếm ở View
            ViewData["CurrentFilter"] = searchString;

            // Lấy danh sách sản phẩm kèm theo thông tin Danh mục
            var products = from p in _context.Products.Include(p => p.Category)
                           select p;

            // Thực hiện lọc nếu người dùng có nhập từ khóa
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) 
                                            || p.ProductCode.Contains(searchString));
            }

            return View(await products.ToListAsync());
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var viewModel = new ViewModels.ProductDetailsViewModel
            {
                Product = product,
                Advice = GenerateProductAdvice(product),
                ColorAdvice = GenerateColorAdvice(product),
                SizeChart = BuildSizeChart(product),
                SizeRecommendation = GenerateSizeRecommendation(product)
            };

            return View(viewModel);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChatAdvice(int id, string question)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return Json(new { success = false, answer = "Không tìm thấy sản phẩm." });
            }

            var answer = GenerateChatbotResponse(product, question);
            return Json(new { success = true, answer });
        }

        private string GenerateProductAdvice(Product product)
        {
            if (product == null) return string.Empty;

            var adviceLines = new List<string>();
            var categoryName = product.Category?.Name ?? "sản phẩm";

            if (product.StockQuantity <= 0)
            {
                adviceLines.Add("Sản phẩm hiện đã hết hàng, bạn nên chọn sản phẩm khác hoặc quay lại sau khi kho cập nhật.");
            }
            else if (product.StockQuantity < 3)
            {
                adviceLines.Add($"Còn ít hàng ({product.StockQuantity} chiếc). Nếu bạn thích sản phẩm này, nên đặt sớm.");
            }

            if (!string.IsNullOrWhiteSpace(product.Status) && product.Status.ToLower() != "available")
            {
                adviceLines.Add($"Trạng thái sản phẩm: {product.Status}. Vui lòng kiểm tra kỹ trước khi thuê.");
            }

            if (product.RentalPricePerDay > 100000)
            {
                adviceLines.Add("Giá thuê ở mức cao, phù hợp với sự kiện quan trọng hoặc sử dụng trong ngắn ngày.");
            }
            else if (product.RentalPricePerDay > 50000)
            {
                adviceLines.Add("Giá thuê vừa phải, phù hợp với nhiều dịp tiệc và sự kiện cá nhân.");
            }

            if (product.Deposit > 0)
            {
                adviceLines.Add($"Tiền cọc {product.Deposit.ToString("N0")} đ. Hãy giữ sản phẩm nguyên vẹn để dễ dàng hoàn trả.");
            }

            if (product.LateFeePerDay > 0)
            {
                adviceLines.Add($"Phí trễ trả mỗi ngày là {product.LateFeePerDay.ToString("N0")} đ. Trả đúng hạn để tiết kiệm chi phí.");
            }

            if (!string.IsNullOrWhiteSpace(product.Material))
            {
                adviceLines.Add($"Chất liệu {product.Material} thường phù hợp cho những tình huống cần sự thoải mái và thanh lịch.");
            }

            if (!string.IsNullOrWhiteSpace(product.Color))
            {
                adviceLines.Add($"Màu {product.Color} dễ phối đồ và phù hợp cho nhiều sự kiện khác nhau.");
            }

            adviceLines.Add($"Sản phẩm thuộc danh mục {categoryName}, hệ thống khuyên bạn cân nhắc theo nhu cầu thực tế.");

            return string.Join(" ", adviceLines);
        }

        private string GenerateColorAdvice(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Color))
            {
                return "Màu sắc sản phẩm chưa rõ ràng. Bạn có thể kiểm tra ảnh thật hoặc hỏi nhân viên để xác định tông màu phù hợp.";
            }

            var color = product.Color.ToLowerInvariant();
            if (color.Contains("đỏ") || color.Contains("hồng") || color.Contains("cam"))
            {
                return "Màu ấm này rất phù hợp cho tiệc tùng, lễ hội hoặc sự kiện cần nổi bật. Kết hợp với phụ kiện trung tính để giữ nét thanh lịch.";
            }
            if (color.Contains("xanh") || color.Contains("lục"))
            {
                return "Màu lạnh như xanh mang lại cảm giác tươi mát, phù hợp với sự kiện ngoài trời và phong cách trẻ trung.";
            }
            if (color.Contains("đen") || color.Contains("trắng") || color.Contains("xám"))
            {
                return "Màu trung tính rất dễ phối, phù hợp cả châu Á lẫn châu Âu và thuận tiện cho nhiều dịp khác nhau.";
            }

            return "Màu này có thể phối được với nhiều phong cách; hãy chọn phụ kiện cùng tông hoặc tương phản nhẹ để tăng điểm nhấn.";
        }

        private string GenerateSizeRecommendation(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.Size))
            {
                return "Chưa có thông tin size rõ ràng. Hãy hỏi thêm nhân viên để chọn size phù hợp theo số đo của bạn.";
            }

            var size = product.Size.Trim().ToUpperInvariant();
            return size switch
            {
                "S" => "Size S thường phù hợp với vóc dáng nhỏ gọn châu Á. Nếu bạn muốn mặc rộng hơn, hãy chọn M.",
                "M" => "Size M là lựa chọn phổ thông, phù hợp với nhiều người châu Á và châu Âu. Nếu bạn muốn ôm nhẹ, có thể chọn S.",
                "L" => "Size L phù hợp với vóc dáng cao hoặc người muốn diện thoải mái. Nếu bạn nhỏ người, hãy cân nhắc size M.",
                _ => $"Size mặc định là {product.Size}. Hãy so sánh với bảng size bên dưới để chọn đúng thông số cho bạn."
            };
        }

        private List<ViewModels.SizeChartEntry> BuildSizeChart(Product product)
        {
            return new List<ViewModels.SizeChartEntry>
            {
                new ViewModels.SizeChartEntry
                {
                    Region = "Châu Á",
                    Chart = "S = 85-90 cm (ngực), M = 90-95 cm, L = 95-100 cm",
                    SizeDetails = new List<ViewModels.SizeDetail>
                    {
                        new ViewModels.SizeDetail
                        {
                            Size = "S",
                            ChestMeasurement = "85-90cm",
                            WeightRange = "45kg-52kg",
                            SleeveLength = "58cm",
                            OtherMeasurements = "41cm (vai), 60cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "M",
                            ChestMeasurement = "90-95cm",
                            WeightRange = "54kg-61kg",
                            SleeveLength = "60cm",
                            OtherMeasurements = "43cm (vai), 65cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "L",
                            ChestMeasurement = "95-100cm",
                            WeightRange = "62kg-69kg",
                            SleeveLength = "62cm",
                            OtherMeasurements = "45cm (vai), 69cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "XL",
                            ChestMeasurement = "100-105cm",
                            WeightRange = "70kg-77kg",
                            SleeveLength = "64cm",
                            OtherMeasurements = "47cm (vai), 71cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "2XL",
                            ChestMeasurement = "105-110cm",
                            WeightRange = "78kg-85kg",
                            SleeveLength = "66cm",
                            OtherMeasurements = "49cm (vai), 73cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "3XL",
                            ChestMeasurement = "110-115cm",
                            WeightRange = "86kg-93kg",
                            SleeveLength = "68cm",
                            OtherMeasurements = "50cm (vai), 75cm (dài áo)"
                        }
                    }
                },
                new ViewModels.SizeChartEntry
                {
                    Region = "Châu Âu",
                    Chart = "S = 90-95 cm (ngực), M = 95-100 cm, L = 100-105 cm",
                    SizeDetails = new List<ViewModels.SizeDetail>
                    {
                        new ViewModels.SizeDetail
                        {
                            Size = "S",
                            ChestMeasurement = "90-95cm",
                            WeightRange = "48kg-55kg",
                            SleeveLength = "59cm",
                            OtherMeasurements = "42cm (vai), 61cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "M",
                            ChestMeasurement = "95-100cm",
                            WeightRange = "56kg-63kg",
                            SleeveLength = "61cm",
                            OtherMeasurements = "44cm (vai), 66cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "L",
                            ChestMeasurement = "100-105cm",
                            WeightRange = "64kg-71kg",
                            SleeveLength = "63cm",
                            OtherMeasurements = "46cm (vai), 70cm (dài áo)"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "XL",
                            ChestMeasurement = "105-110cm",
                            WeightRange = "72kg-79kg",
                            SleeveLength = "65cm",
                            OtherMeasurements = "48cm (vai), 72cm (dài áo)"
                        }
                    }
                },
                new ViewModels.SizeChartEntry
                {
                    Region = "UK/US",
                    Chart = "S = 36-38, M = 38-40, L = 40-42",
                    SizeDetails = new List<ViewModels.SizeDetail>
                    {
                        new ViewModels.SizeDetail
                        {
                            Size = "S (36-38)",
                            ChestMeasurement = "88-90 inches",
                            WeightRange = "45kg-52kg",
                            SleeveLength = "32-33 inches",
                            OtherMeasurements = "Phù hợp với người mảnh khảnh"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "M (38-40)",
                            ChestMeasurement = "90-92 inches",
                            WeightRange = "54kg-64kg",
                            SleeveLength = "33-34 inches",
                            OtherMeasurements = "Phù hợp với người bình thường"
                        },
                        new ViewModels.SizeDetail
                        {
                            Size = "L (40-42)",
                            ChestMeasurement = "92-94 inches",
                            WeightRange = "65kg-75kg",
                            SleeveLength = "34-35 inches",
                            OtherMeasurements = "Phù hợp với người to hơn"
                        }
                    }
                }
            };
        }

        private string GenerateChatbotResponse(Product product, string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Xin chào! Tôi là trợ lý size & màu sắc. Bạn có thể hỏi:\n- 'Tôi nặng 55kg, vóc dáng như thế nào thì nên lấy size nào?'\n- 'Màu này hợp với da ngăm/trắng không?'\n- 'Sự kiện gì thì mặc được?'";
            }

            var normalized = question.Trim().ToLowerInvariant();

            // ===== TƯ VẤN SIZE =====
            if (normalized.Contains("size") || normalized.Contains("cỡ") || normalized.Contains("kích cỡ") || normalized.Contains("vóc"))
            {
                return GetSizeAdvice(product, question);
            }

            if (normalized.Contains("châu á") || normalized.Contains("asia"))
            {
                return $"Size châu Á phù hợp với vóc dáng người Việt, người Thái, người Hàn. Với sản phẩm này (size {product.Size}):\n- Nếu bạn 48-52kg: chọn S\n- Nếu 52-60kg: chọn M\n- Nếu 60kg+: chọn L\n\nBảng tham khảo: S=85-90cm, M=90-95cm, L=95-100cm (vòng ngực).";
            }

            if (normalized.Contains("châu âu") || normalized.Contains("europe"))
            {
                return $"Size châu Âu to hơn châu Á. Công thức chuyển đổi: "+ 
                       $"\n- Nếu bạn lấy S châu Á, chọn S châu Âu vẫn được\n" +
                       $"- Nếu bạn lấy M châu Á, có thể chọn S-M châu Âu\n" +
                       $"Bảng: S=90-95cm, M=95-100cm, L=100-105cm (vòng ngực)";
            }

            // ===== TƯ VẤN MÀU SẮC =====
            if (normalized.Contains("màu") || normalized.Contains("phối") || normalized.Contains("da") || normalized.Contains("mặc"))
            {
                return GetColorAdvice(product, question);
            }

            if (normalized.Contains("trắng") || normalized.Contains("sáng"))
            {
                return $"Sản phẩm này màu {product.Color}:\n" +
                       "- Nếu da bạn trắng/sáng: Màu sáng sẽ làm bạn nổi bật, tươi trẻ. Kết hợp phụ kiện tương tự để thanh lịch.\n" +
                       "- Nếu da bạn ngăm: Màu sáng sẽ tạo nét tương phản, góc cạnh. Hãy chọn phụ kiện cùng tông để mềm mại hơn.";
            }

            if (normalized.Contains("ngăm") || normalized.Contains("nâu"))
            {
                return $"Sản phẩm này màu {product.Color}:\n" +
                       "- Nếu da bạn ngăm/nâu: Màu tối sẽ tạo sự nhất quán, thanh lịch, không lô lăng.\n" +
                       "- Nếu da bạn trắng/sáng: Màu tối sẽ hơi nặng, nhưng rất sang trọng. Thêm phụ kiện sáng để cân bằng.";
            }

            // ===== TƯ VẤN SỰ KIỆN =====
            if (normalized.Contains("sự kiện") || normalized.Contains("tiệc") || normalized.Contains("đám cưới") || normalized.Contains("chụp ảnh") || normalized.Contains("hẹn hò"))
            {
                return GetEventAdvice(product, question);
            }

            // ===== TƯ VẤN CỌC VÀ GIÁ =====
            if (normalized.Contains("cọc") || normalized.Contains("giá") || normalized.Contains("bao nhiêu") || normalized.Contains("tiền"))
            {
                return $"Chi phí cho sản phẩm này:\n" +
                       $"- Giá thuê: {product.RentalPricePerDay:N0} đ/ngày\n" +
                       $"- Tiền cọc: {product.Deposit:N0} đ (hoàn khi trả đúng hạn)\n" +
                       $"- Phí trễ: {product.LateFeePerDay:N0} đ/ngày (nếu trả muộn)\n" +
                       $"\nBạn có thể thuê 3-7 ngày. Nếu thuê lâu, hỏi về chương trình ưu đãi.";
            }

            // ===== CÂU HỎI CHUNG KHÁC =====
            return "Tôi chưa hiểu rõ câu hỏi của bạn. Bạn có thể hỏi cụ thể hơn về:\n" +
                   "- Size (vóc dáng, cân nặng của bạn)\n" +
                   "- Màu sắc (tông da, phù hợp không)\n" +
                   "- Sự kiện (dùng cho dịp gì)\n" +
                   "- Giá cộc & thanh toán";
        }

        private string GetSizeAdvice(Product product, string question)
        {
            var q = question.ToLowerInvariant();
            
            // Lấy bảng size để tham khảo
            var sizeChart = BuildSizeChart(product);
            
            // ===== PARSE THÔNG TIN CÂN NẶNG =====
            int? weight = null;
            if (q.Contains("kg") || q.Contains("cân nặng"))
            {
                var numbers = System.Text.RegularExpressions.Regex.Matches(question, @"\d+");
                if (numbers.Count > 0 && int.TryParse(numbers[0].Value, out int w))
                {
                    weight = w;
                }
            }

            // ===== NẾU CÓ CÂN NẶNG: TÌM SIZE PHÙHỢP =====
            if (weight.HasValue)
            {
                var asiaChart = sizeChart.FirstOrDefault(x => x.Region == "Châu Á");
                if (asiaChart != null)
                {
                    // Tìm size phù hợp với cân nặng
                    var matchedSize = asiaChart.SizeDetails.FirstOrDefault(s =>
                    {
                        if (string.IsNullOrEmpty(s.WeightRange)) return false;
                        // Parse "45kg-52kg" format
                        var parts = s.WeightRange.Split('-');
                        if (parts.Length >= 2)
                        {
                            int.TryParse(parts[0].Replace("kg", ""), out int minW);
                            int.TryParse(parts[1].Replace("kg", ""), out int maxW);
                            return weight >= minW && weight <= maxW;
                        }
                        return false;
                    });

                    if (matchedSize != null)
                    {
                        return $"✅ **Với cân nặng {weight}kg, bạn nên chọn size {matchedSize.Size}**!\n\n" +
                               $"📏 Chi tiết size {matchedSize.Size}:\n" +
                               $"   • Vòng ngực: {matchedSize.ChestMeasurement}\n" +
                               $"   • Trọng lượng: {matchedSize.WeightRange}\n" +
                               $"   • Dài tay: {matchedSize.SleeveLength}\n" +
                               $"   • {matchedSize.OtherMeasurements}\n\n" +
                               $"💡 Sản phẩm này size {product.Size}. " +
                               (product.Size.ToUpper() == matchedSize.Size.ToUpper() 
                                   ? "Hoàn hảo cho bạn!" 
                                   : $"Nếu không có size {matchedSize.Size}, bạn có thể xem xét {product.Size}.");
                    }
                }

                // Fallback nếu không tìm được từ dataset
                if (weight < 50)
                    return $"✅ **Với cân nặng {weight}kg, bạn phù hợp với size S**\n\n" +
                           $"📊 Bảng size Châu Á:\n" +
                           $"   • S: 85-90cm ngực, 45-52kg ✓ (bạn ở đây)\n" +
                           $"   • M: 90-95cm ngực, 54-61kg\n" +
                           $"   • L: 95-100cm ngực, 62-69kg\n\n" +
                           $"Chọn size S sẽ vừa vặn nhất!";

                if (weight < 65)
                    return $"✅ **Với cân nặng {weight}kg, bạn phù hợp với size M**\n\n" +
                           $"📊 Bảng size Châu Á:\n" +
                           $"   • S: 85-90cm ngực, 45-52kg\n" +
                           $"   • M: 90-95cm ngực, 54-61kg ✓ (bạn ở đây)\n" +
                           $"   • L: 95-100cm ngực, 62-69kg\n\n" +
                           $"Size M thoải mái, không chật cũng không rộng!";

                if (weight < 75)
                    return $"✅ **Với cân nặng {weight}kg, bạn phù hợp với size L**\n\n" +
                           $"📊 Bảng size Châu Á:\n" +
                           $"   • M: 90-95cm ngực, 54-61kg\n" +
                           $"   • L: 95-100cm ngực, 62-69kg ✓ (bạn ở đây)\n" +
                           $"   • XL: 100-105cm ngực, 70-77kg\n\n" +
                           $"Size L thoải mái cho vóc dáng cao!";

                return $"✅ **Với cân nặng {weight}kg, bạn phù hợp với size XL hoặc lớn hơn**\n\n" +
                       $"📊 Bảng size Châu Á:\n" +
                       $"   • L: 95-100cm ngực, 62-69kg\n" +
                       $"   • XL: 100-105cm ngực, 70-77kg ✓ (bạn ở đây)\n" +
                       $"   • 2XL: 105-110cm ngực, 78-85kg\n\n" +
                       $"Chọn size lớn sẽ thoải mái!";
            }

            // ===== NẾU KHÁCH HỎI VỀ VÓC DÁNG CHỨ KHÔNG CÓ TRỌNG LƯỢNG =====
            if (q.Contains("nhỏ") || q.Contains("mảnh") || q.Contains("gầy"))
            {
                return $"👤 **Vóc dáng nhỏ gọn? Chọn size S**!\n\n" +
                       $"📏 Size S chi tiết:\n" +
                       $"   • Vòng ngực: 85-90cm (Châu Á) / 90-95cm (Châu Âu)\n" +
                       $"   • Trọng lượng phù hợp: 45-52kg\n" +
                       $"   • Vai: 41cm\n\n" +
                       $"Nếu không có S, size M cũng có thể nhưng sẽ hơi rộng.";
            }

            if (q.Contains("vừa") || q.Contains("bình thường") || q.Contains("vừa vặn"))
            {
                return $"👤 **Vóc dáng bình thường? Chọn size M**!\n\n" +
                       $"📏 Size M chi tiết:\n" +
                       $"   • Vòng ngực: 90-95cm (Châu Á) / 95-100cm (Châu Âu)\n" +
                       $"   • Trọng lượng phù hợp: 54-61kg\n" +
                       $"   • Vai: 43cm\n\n" +
                       $"Size M rất thoải mái, không chật!";
            }

            if (q.Contains("cao") || q.Contains("to") || q.Contains("rộng") || q.Contains("khỏe"))
            {
                return $"👤 **Vóc dáng cao/to? Chọn size L hoặc XL**!\n\n" +
                       $"📏 Size L chi tiết:\n" +
                       $"   • Vòng ngực: 95-100cm (Châu Á) / 100-105cm (Châu Âu)\n" +
                       $"   • Trọng lượng phù hợp: 62-69kg\n" +
                       $"   • Vai: 45cm\n\n" +
                       $"📏 Size XL nếu muốn rộng hơn:\n" +
                       $"   • Vòng ngực: 100-105cm\n" +
                       $"   • Trọng lượng: 70-77kg\n" +
                       $"   • Vai: 47cm";
            }

            // ===== HIỂN THỊ BẢNG SIZE ĐẦY ĐỦ NẾU KHÔNG BIẾT =====
            return $"📊 **Bảng size chi tiết - Châu Á:**\n" +
                   $"   • S: 85-90cm ngực | 45-52kg | Vai 41cm | Dài áo 60cm\n" +
                   $"   • M: 90-95cm ngực | 54-61kg | Vai 43cm | Dài áo 65cm ← Phổ biến nhất\n" +
                   $"   • L: 95-100cm ngực | 62-69kg | Vai 45cm | Dài áo 69cm\n" +
                   $"   • XL: 100-105cm ngực | 70-77kg | Vai 47cm | Dài áo 71cm\n\n" +
                   $"💡 **Hãy cho tôi biết:**\n" +
                   $"   • Cân nặng của bạn (kg)\n" +
                   $"   • Vóc dáng (nhỏ/vừa/cao/to)\n" +
                   $"   • Đo vòng ngực của bạn (cm)\n" +
                   $"Tôi sẽ tư vấn size phù hợp!";
        }

        private string GetColorAdvice(Product product, string question)
        {
            var q = question.ToLowerInvariant();
            var color = product.Color?.ToLowerInvariant() ?? "không rõ";

            // Kiểm tra tông da
            if (q.Contains("da trắng") || q.Contains("da sáng") || q.Contains("trắng"))
            {
                if (color.Contains("đỏ") || color.Contains("hồng") || color.Contains("cam"))
                    return $"Với da trắng, màu {product.Color} rất phù hợp! Nó sẽ làm da bạn nổi bật, sáng khỏe. " +
                           "Kết hợp phụ kiện cùng tông hoặc tương phản nhẹ để thêm điểm nhấn.";
                           
                if (color.Contains("xanh") || color.Contains("lục"))
                    return $"Với da trắng, màu {product.Color} rất tươi mát! Tạo nên vẻ thanh lịch, nữ tính. " +
                           "Phụ kiện vàng hoặc bạc đều phù hợp.";
                           
                if (color.Contains("đen") || color.Contains("trắng") || color.Contains("xám"))
                    return $"Với da trắng, màu {product.Color} luôn an toàn và sang trọng. " +
                           "Bạn có thể phối được với mọi trang phục khác.";
            }

            if (q.Contains("da ngăm") || q.Contains("da nâu") || q.Contains("da sẫm") || q.Contains("ngăm"))
            {
                if (color.Contains("đỏ") || color.Contains("hồng") || color.Contains("cam"))
                    return $"Với da ngăm, màu {product.Color} rất nổi bật, góc cạnh! Tạo nên sự tương phản đẹp mắt. " +
                           "Thêm phụ kiện vàng hoặc đất để nổi bật hơn.";
                           
                if (color.Contains("xanh") || color.Contains("lục"))
                    return $"Với da ngăm, màu {product.Color} vừa tươi vừa thanh lịch. " +
                           "Không bị mất căng, phù hợp cho dạo phố hoặc sự kiện nhẹ nhàng.";
                           
                if (color.Contains("đen") || color.Contains("xám"))
                    return $"Với da ngăm, màu {product.Color} rất nhất quán, sang trọng và dễ phối. " +
                           "Thêm phụ kiện sáng (vàng, bạc) để cân bằng.";
            }

            // Trả lời chung
            return $"Sản phẩm này màu {product.Color}:\n" +
                   "- Nếu da bạn trắng/sáng: sẽ tạo tương phản, nổi bật\n" +
                   "- Nếu da bạn ngăm/nâu: sẽ tạo nhất quán, sang trọng\n" +
                   "Hãy cho mình biết tông da để tôi tư vấn cụ thể hơn!";
        }

        private string GetEventAdvice(Product product, string question)
        {
            var q = question.ToLowerInvariant();
            var category = product.Category?.Name?.ToLowerInvariant() ?? "đồ";

            if (q.Contains("đám cưới"))
            {
                return $"Cho đám cưới: sản phẩm này ({category}) " +
                       $"màu {product.Color}, rất thanh lịch và sang trọng. " +
                       "Kết hợp phụ kiện váy/yếm tương tự, trang sức bạc hoặc vàng nhẹ để không quá nhiều.";
            }

            if (q.Contains("chụp ảnh") || q.Contains("kỷ niệm"))
            {
                return $"Cho chụp ảnh: sản phẩm này ({category}) " +
                       $"màu {product.Color} sẽ rất đẹp trên ảnh, không bị phai, " +
                       "tạo nên vẻ tươi sáng và ấn tượng. Chọn backdrop hoặc phông nền phù hợp màu này.";
            }

            if (q.Contains("hẹn hò") || q.Contains("đi chơi"))
            {
                return $"Cho hẹn hò: sản phẩm này ({category}) " +
                       $"màu {product.Color} rất thích hợp, vừa thanh lịch vừa trẻ trung. " +
                       "Dễ dàng kết hợp với sneaker, giày da hoặc sandal tùy theo phong cách bạn.";
            }

            // Trả lời chung
            return $"Sản phẩm này ({category}) " +
                   $"màu {product.Color} rất linh hoạt. " +
                   "Bạn có thể mặc cho: đám cưới, tiệc tùng, chụp ảnh, hẹn hò, dịp lễ hội. " +
                   "Hãy cho mình biết sự kiện cụ thể để tư vấn thêm chi tiết!";
        }

        // ==========================================================
        // CÁC CHỨC NĂNG QUẢN TRỊ - CHỈ DÀNH CHO ADMIN
        // ==========================================================

        // 1. GET: Tạo sản phẩm mới (Đã nạp danh mục để click chọn)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // Load danh sách danh mục vào ViewBag để dropdown ở View hiển thị được
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name");
            return View();
        }

        // 2. POST: Lưu sản phẩm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Nếu có lỗi, nạp lại danh mục để dropdown không bị trống
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 3. GET: Chỉnh sửa sản phẩm (Đã nạp danh mục để chọn lại)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Cực kỳ quan trọng: Nạp danh mục để dropdown "Sửa sản phẩm" hiển thị dữ liệu
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 4. POST: Cập nhật sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            // Nạp lại danh mục nếu lưu thất bại để tránh lỗi dropdown trống
            ViewBag.CategoryId = new SelectList(_context.ProductCategories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 5. GET: Xóa sản phẩm
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // 6. POST: Xác nhận xóa sản phẩm
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}