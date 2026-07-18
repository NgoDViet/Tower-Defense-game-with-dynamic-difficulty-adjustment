Tower Defense Game with Dynamic Difficulty Adjustment (DDA)

Giới thiệu
Đây là dự án Đồ án tốt nghiệp phát triển game Tower Defense tích hợp công nghệ AI điều chỉnh độ khó động thời gian thực (Dynamic Difficulty Adjustment - DDA), được xây dựng trên nền tảng Unity Engine.

Dự án không chỉ tập trung vào việc mô phỏng lối chơi thủ thành cổ điển một cách mượt mà và tối ưu hiệu năng, mà còn tích hợp hệ thống AI thông minh tự động tinh chỉnh các thuộc tính của quái vật, số lượng quái vật và tần suất xuất hiện của các đợt tấn công (waves) dựa trên kỹ năng và trạng thái thời gian thực của người chơi (Máu của nhà chính, lượng Vàng tích lũy, số lượng tháp canh, tốc độ tiêu diệt địch).

Các tính năng chính

1. Hệ thống Gameplay Thủ thành Hoàn chỉnh
- Xây dựng và Kéo thả tháp: Người chơi có thể lựa chọn tháp và kéo thả vào bản đồ để thiết lập tuyến phòng thủ.
- Các chế độ nhắm mục tiêu: Tháp canh hỗ trợ tấn công mục tiêu đầu tiên, mục tiêu gần nhất, hoặc mục tiêu mạnh nhất.
- Dữ liệu linh hoạt: Toàn bộ thông số tháp canh, quái vật, đợt quái và màn chơi được cấu hình thông qua ScriptableObjects giúp dễ dàng mở rộng và cân bằng game.

2. Cơ chế Điều chỉnh Độ khó Động (Real-time DDA AI)
- Hệ thống liên tục thu thập dữ liệu chơi như máu còn lại của căn cứ, lượng vàng hiện có, số lượng tháp canh đã xây, và tốc độ tiêu diệt kẻ địch.
- Phân tích hiệu suất của người chơi để tự động điều chỉnh chỉ số của kẻ địch (Máu, Tốc độ di chuyển, Phần thưởng vàng) hoặc thay đổi cấu trúc Wave để đảm bảo game luôn có độ thử thách tối ưu (không quá dễ gây nhàm chán và không quá khó gây ức chế).

3. Kiến trúc Lập trình Event-Driven
- Sử dụng EventBus trung gian giúp các thành phần (Core, UI, Enemy, Tower) liên kết với nhau một cách lỏng lẻo.
- Các sự kiện quan trọng như EnemySpawnedEvent, EnemyDiedEvent, EnemyReachedBaseEvent, BaseHealthChangedEvent giúp đồng bộ hóa dữ liệu và cập nhật giao diện thời gian thực cực kỳ hiệu quả.

4. Tối ưu hóa Hiệu năng với Object Pooling
- Tái sử dụng quái vật và đạn bắn thông qua hệ thống ObjectPooler chung để hạn chế tối đa việc gọi Instantiate và Destroy liên tục, giảm thiểu hiện tượng giật lag khi số lượng vật thể trên màn hình quá lớn.

5. Công cụ Hỗ trợ Editor chuyên nghiệp
- Tích hợp Menu Editor Window giúp nhà phát triển nhanh chóng khởi tạo toàn bộ Prefab mẫu, ScriptableObjects, WaypointPath và cấu hình nhanh một màn chơi demo hoàn chỉnh chỉ trong 1 click.

Cấu trúc thư mục Source Code

Dự án được cấu trúc rõ ràng trong thư mục Assets/Scripts:
- Core: Chứa các GameManager quản lý trạng thái trò chơi, WaveManager điều phối spawn quái, EventBus và các sự kiện chung.
- Data: Chứa các lớp ScriptableObject (LevelData, WaveData, EnemyData, TowerData) để cấu hình thông số.
- Enemy: Quản lý logic di chuyển theo WaypointPath (EnemyMovement), tính toán máu (EnemyHealth), vẽ đường đi trực quan trong Editor.
- Tower: Quản lý tầm bắn, cơ chế xoay hướng và bắn đạn (TowerController), đặt tháp canh (TowerPlacementManager).
- Projectile: Xử lý đạn bay tìm mục tiêu và gây sát thương (ProjectileController).
- Pooling: Hệ thống quản lý pool đối tượng (ObjectPooler) tối ưu hiệu năng.
- UI: Điều phối các màn hình Main Menu, Pause, HUD thông tin hiển thị và kéo thả tháp.
- Editor: Custom editor wizard (DemoSetupWizard) hỗ trợ dựng nhanh môi trường test game.

Công nghệ sử dụng
- Engine: Unity 2022+ / Unity 6
- Ngôn ngữ: C# (NET Standard)
- Input: Unity New Input System
- UI: TextMesh Pro và Unity UI (UGUI)

Hướng dẫn cài đặt và Chạy thử

1. Import dự án vào Unity:
- Mở Unity Hub, chọn Add project from disk và trỏ đến thư mục dự án này.
- Sử dụng phiên bản Unity tương thích (khuyến nghị từ 2022.3 LTS trở lên).

2. Khởi tạo dữ liệu Demo:
- Trên thanh menu của Unity Editor, chọn Tower Defense -> Setup Playable Demo.
- Cửa sổ xác nhận xuất hiện, nhấn Yes, Setup Demo để tự động sinh các Prefab mẫu và cấu hình màn chơi.

3. Chạy game:
- Mở màn chơi vừa tạo tại Assets/Scenes/Level 1.unity (hoặc mở Assets/Scenes/LevelDemo.unity).
- Nhấn nút Play trong Unity để trải nghiệm thử.
