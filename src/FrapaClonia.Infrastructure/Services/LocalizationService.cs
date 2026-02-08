using FrapaClonia.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FrapaClonia.Infrastructure.Services;

/// <summary>
/// Service for managing application localization
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly Dictionary<string, Dictionary<string, string>> _localizationData;

    public CultureInfo CurrentCulture { get; private set; }
    public List<CultureInfo> SupportedCultures { get; }

    public event EventHandler? CultureChanged;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;

        SupportedCultures =
        [
            new("en"), // English
            new("zh-CN"), // Chinese Simplified
            new("zh-TW"), // Chinese Traditional
            new("ja"), // Japanese
            new("ko"), // Korean
            new("es"), // Spanish
            new("fr"), // French
            new("de"), // German
            new("ru")
        ];

        _localizationData = new Dictionary<string, Dictionary<string, string>>();
        InitializeLocalizationData();

        // Auto-detect system language
        var systemCulture = CultureInfo.CurrentUICulture;
        var supportedCulture = SupportedCultures
            .FirstOrDefault(c => c.Name == systemCulture.Name || c.Name.StartsWith(systemCulture.TwoLetterISOLanguageName));

        CurrentCulture = supportedCulture ?? SupportedCultures[0];

        _logger.LogInformation("Localization initialized with culture: {Culture}", CurrentCulture.Name);
    }

    public void SetCulture(string cultureCode)
    {
        var culture = SupportedCultures.FirstOrDefault(c => c.Name == cultureCode);
        if (culture != null)
        {
            CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureChanged?.Invoke(this, EventArgs.Empty);
            _logger.LogInformation("Culture changed to: {Culture}", culture.Name);
        }
    }

    public string GetString(string key, params object[] args)
    {
        var cultureCode = CurrentCulture.Name;

        if (_localizationData.TryGetValue(cultureCode, out var strings))
        {
            if (strings.TryGetValue(key, out var value))
            {
                return args.Length > 0 ? string.Format(value, args) : value;
            }
        }

        // Fallback to English
        if (_localizationData.TryGetValue("en", out var englishStrings))
        {
            if (englishStrings.TryGetValue(key, out var englishValue))
            {
                return args.Length > 0 ? string.Format(englishValue, args) : englishValue;
            }
        }

        // Return key if not found
        _logger.LogWarning("Localization key not found: {Key}", key);
        return key;
    }

    private void InitializeLocalizationData()
    {
        // English (default)
        _localizationData["en"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "Dashboard",
            ["ServerConfig"] = "Server Configuration",
            ["ProxyManagement"] = "Proxy Management",
            ["Deployment"] = "Frpc Deployment",
            ["Settings"] = "Settings",
            ["Logs"] = "Logs",
            ["About"] = "About",
            ["StartFrpc"] = "Start Frpc",
            ["StopFrpc"] = "Stop Frpc",
            ["RestartFrpc"] = "Restart Frpc",
            ["AddProxy"] = "Add Proxy",
            ["EditProxy"] = "Edit Proxy",
            ["DeleteProxy"] = "Delete Proxy",
            ["DuplicateProxy"] = "Duplicate Proxy",
            ["ClearAll"] = "Clear All",
            ["Save"] = "Save",
            ["Cancel"] = "Cancel",
            ["Refresh"] = "Refresh",
            ["Export"] = "Export",
            ["Import"] = "Import",
            ["ProxyName"] = "Proxy Name",
            ["ProxyType"] = "Proxy Type",
            ["LocalIP"] = "Local IP",
            ["LocalPort"] = "Local Port",
            ["RemotePort"] = "Remote Port",
            ["CustomDomains"] = "Custom Domains",
            ["Subdomain"] = "Subdomain",
            ["SecretKey"] = "Secret Key",
            ["ServerAddress"] = "Server Address",
            ["ServerPort"] = "Server Port",
            ["Token"] = "Token",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "Protocol",
            ["EnableTLS"] = "Enable TLS",
            ["HealthCheck"] = "Health Check",
            ["Plugin"] = "Plugin",
            ["EnableEncryption"] = "Use Encryption",
            ["EnableCompression"] = "Use Compression",
            ["FrpcStatus"] = "Frpc Status",
            ["FrpcRunning"] = "Frpc is running",
            ["FrpcNotRunning"] = "Frpc is not running",
            ["ProcessId"] = "Process ID",
            ["Configuration"] = "Configuration",
            ["Visitors"] = "Visitors",
            ["AddVisitor"] = "Add Visitor",
            ["EditVisitor"] = "Edit Visitor",
            ["DeleteVisitor"] = "Delete Visitor",
            ["VisitorName"] = "Visitor Name",
            ["VisitorType"] = "Visitor Type",
            ["ServerName"] = "Server Name",
            ["BindAddr"] = "Bind Address",
            ["BindPort"] = "Bind Port",
            ["Language"] = "Language",
            ["AutoStart"] = "Start on Boot",
            ["PortableMode"] = "Portable Mode",
            ["QuickShare"] = "Quick Share"
        };

        // Chinese Simplified (zh-CN)
        _localizationData["zh-CN"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "仪表板",
            ["ServerConfig"] = "服务器配置",
            ["ProxyManagement"] = "代理管理",
            ["Deployment"] = "Frpc 部署",
            ["Settings"] = "设置",
            ["Logs"] = "日志",
            ["About"] = "关于",
            ["StartFrpc"] = "启动 Frpc",
            ["StopFrpc"] = "停止 Frpc",
            ["RestartFrpc"] = "重启 Frpc",
            ["AddProxy"] = "添加代理",
            ["EditProxy"] = "编辑代理",
            ["DeleteProxy"] = "删除代理",
            ["DuplicateProxy"] = "复制代理",
            ["ClearAll"] = "清空全部",
            ["Save"] = "保存",
            ["Cancel"] = "取消",
            ["Refresh"] = "刷新",
            ["Export"] = "导出",
            ["Import"] = "导入",
            ["ProxyName"] = "代理名称",
            ["ProxyType"] = "代理类型",
            ["LocalIP"] = "本地 IP",
            ["LocalPort"] = "本地端口",
            ["RemotePort"] = "远程端口",
            ["CustomDomains"] = "自定义域名",
            ["Subdomain"] = "子域名",
            ["SecretKey"] = "密钥",
            ["ServerAddress"] = "服务器地址",
            ["ServerPort"] = "服务器端口",
            ["Token"] = "令牌",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "协议",
            ["EnableTLS"] = "启用 TLS",
            ["HealthCheck"] = "健康检查",
            ["Plugin"] = "插件",
            ["EnableEncryption"] = "使用加密",
            ["EnableCompression"] = "使用压缩",
            ["FrpcStatus"] = "Frpc 状态",
            ["FrpcRunning"] = "Frpc 正在运行",
            ["FrpcNotRunning"] = "Frpc 未运行",
            ["ProcessId"] = "进程 ID",
            ["Configuration"] = "配置",
            ["Visitors"] = "访问者",
            ["AddVisitor"] = "添加访问者",
            ["EditVisitor"] = "编辑访问者",
            ["DeleteVisitor"] = "删除访问者",
            ["VisitorName"] = "访问者名称",
            ["VisitorType"] = "访问者类型",
            ["ServerName"] = "服务器名称",
            ["BindAddr"] = "绑定地址",
            ["BindPort"] = "绑定端口",
            ["Language"] = "语言",
            ["AutoStart"] = "开机启动",
            ["PortableMode"] = "便携模式",
            ["QuickShare"] = "快速分享"
        };

        // Chinese Traditional (zh-TW)
        _localizationData["zh-TW"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "儀表板",
            ["ServerConfig"] = "伺服器設定",
            ["ProxyManagement"] = "代理管理",
            ["Deployment"] = "Frpc 部署",
            ["Settings"] = "設定",
            ["Logs"] = "日誌",
            ["About"] = "關於",
            ["StartFrpc"] = "啟動 Frpc",
            ["StopFrpc"] = "停止 Frpc",
            ["RestartFrpc"] = "重新啟動 Frpc",
            ["AddProxy"] = "新增代理",
            ["EditProxy"] = "編輯代理",
            ["DeleteProxy"] = "刪除代理",
            ["DuplicateProxy"] = "複製代理",
            ["ClearAll"] = "清空全部",
            ["Save"] = "儲存",
            ["Cancel"] = "取消",
            ["Refresh"] = "重新整理",
            ["Export"] = "匯出",
            ["Import"] = "匯入",
            ["ProxyName"] = "代理名稱",
            ["ProxyType"] = "代理類型",
            ["LocalIP"] = "本地 IP",
            ["LocalPort"] = "本地埠",
            ["RemotePort"] = "遠端埠",
            ["CustomDomains"] = "自訂網域",
            ["Subdomain"] = "子網域",
            ["SecretKey"] = "金鑰",
            ["ServerAddress"] = "伺服器位址",
            ["ServerPort"] = "伺服器埠",
            ["Token"] = "權杖",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "通訊協定",
            ["EnableTLS"] = "啟用 TLS",
            ["HealthCheck"] = "健康檢查",
            ["Plugin"] = "外掛程式",
            ["EnableEncryption"] = "使用加密",
            ["EnableCompression"] = "使用壓縮",
            ["FrpcStatus"] = "Frpc 狀態",
            ["FrpcRunning"] = "Frpc 正在執行",
            ["FrpcNotRunning"] = "Frpc 未執行",
            ["ProcessId"] = "程序 ID",
            ["Configuration"] = "設定",
            ["Visitors"] = "訪客",
            ["AddVisitor"] = "新增訪客",
            ["EditVisitor"] = "編輯訪客",
            ["DeleteVisitor"] = "刪除訪客",
            ["VisitorName"] = "訪客名稱",
            ["VisitorType"] = "訪客類型",
            ["ServerName"] = "伺服器名稱",
            ["BindAddr"] = "綁定位址",
            ["BindPort"] = "綁定埠",
            ["Language"] = "語言",
            ["AutoStart"] = "開機啟動",
            ["PortableMode"] = "便攜模式",
            ["QuickShare"] = "快速分享"
        };

        // Japanese (ja)
        _localizationData["ja"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "ダッシュボード",
            ["ServerConfig"] = "サーバー設定",
            ["ProxyManagement"] = "プロキシ管理",
            ["Deployment"] = "Frpc デプロイ",
            ["Settings"] = "設定",
            ["Logs"] = "ログ",
            ["About"] = "について",
            ["StartFrpc"] = "Frpc 開始",
            ["StopFrpc"] = "Frpc 停止",
            ["RestartFrpc"] = "Frpc 再起動",
            ["AddProxy"] = "プロキシ追加",
            ["EditProxy"] = "プロキシ編集",
            ["DeleteProxy"] = "プロキシ削除",
            ["DuplicateProxy"] = "プロキシ複製",
            ["ClearAll"] = "すべてクリア",
            ["Save"] = "保存",
            ["Cancel"] = "キャンセル",
            ["Refresh"] = "更新",
            ["Export"] = "エクスポート",
            ["Import"] = "インポート",
            ["ProxyName"] = "プロキシ名",
            ["ProxyType"] = "プロキシタイプ",
            ["LocalIP"] = "ローカル IP",
            ["LocalPort"] = "ローカルポート",
            ["RemotePort"] = "リモートポート",
            ["CustomDomains"] = "カスタムドメイン",
            ["Subdomain"] = "サブドメイン",
            ["SecretKey"] = "シークレットキー",
            ["ServerAddress"] = "サーバーアドレス",
            ["ServerPort"] = "サーバーポート",
            ["Token"] = "トークン",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "プロトコル",
            ["EnableTLS"] = "TLS 有効化",
            ["HealthCheck"] = "ヘルスチェック",
            ["Plugin"] = "プラグイン",
            ["EnableEncryption"] = "暗号化を使用",
            ["EnableCompression"] = "圧縮を使用",
            ["FrpcStatus"] = "Frpc 状態",
            ["FrpcRunning"] = "Frpc 実行中",
            ["FrpcNotRunning"] = "Frpc 停止中",
            ["ProcessId"] = "プロセス ID",
            ["Configuration"] = "設定",
            ["Visitors"] = "ビジター",
            ["AddVisitor"] = "ビジター追加",
            ["EditVisitor"] = "ビジター編集",
            ["DeleteVisitor"] = "ビジター削除",
            ["VisitorName"] = "ビジター名",
            ["VisitorType"] = "ビジタータイプ",
            ["ServerName"] = "サーバー名",
            ["BindAddr"] = "バインドアドレス",
            ["BindPort"] = "バインドポート",
            ["Language"] = "言語",
            ["AutoStart"] = "起動時に開始",
            ["PortableMode"] = "ポータブルモード",
            ["QuickShare"] = "クイック共有"
        };

        // Korean (ko)
        _localizationData["ko"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "대시보드",
            ["ServerConfig"] = "서버 구성",
            ["ProxyManagement"] = "프록시 관리",
            ["Deployment"] = "Frpc 배포",
            ["Settings"] = "설정",
            ["Logs"] = "로그",
            ["About"] = "정보",
            ["StartFrpc"] = "Frpc 시작",
            ["StopFrpc"] = "Frpc 중지",
            ["RestartFrpc"] = "Frpc 재시작",
            ["AddProxy"] = "프록시 추가",
            ["EditProxy"] = "프록시 편집",
            ["DeleteProxy"] = "프록시 삭제",
            ["DuplicateProxy"] = "프록시 복제",
            ["ClearAll"] = "모두 지우기",
            ["Save"] = "저장",
            ["Cancel"] = "취소",
            ["Refresh"] = "새로고침",
            ["Export"] = "내보내기",
            ["Import"] = "가져오기",
            ["ProxyName"] = "프록시 이름",
            ["ProxyType"] = "프록시 유형",
            ["LocalIP"] = "로컬 IP",
            ["LocalPort"] = "로컬 포트",
            ["RemotePort"] = "원격 포트",
            ["CustomDomains"] = "사용자 지정 도메인",
            ["Subdomain"] = "서브도메인",
            ["SecretKey"] = "비밀 키",
            ["ServerAddress"] = "서버 주소",
            ["ServerPort"] = "서버 포트",
            ["Token"] = "토큰",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "프로토콜",
            ["EnableTLS"] = "TLS 사용",
            ["HealthCheck"] = "상태 확인",
            ["Plugin"] = "플러그인",
            ["EnableEncryption"] = "암호화 사용",
            ["EnableCompression"] = "압축 사용",
            ["FrpcStatus"] = "Frpc 상태",
            ["FrpcRunning"] = "Frpc 실행 중",
            ["FrpcNotRunning"] = "Frpc 실행 중 아님",
            ["ProcessId"] = "프로세스 ID",
            ["Configuration"] = "구성",
            ["Visitors"] = "방문자",
            ["AddVisitor"] = "방문자 추가",
            ["EditVisitor"] = "방문자 편집",
            ["DeleteVisitor"] = "방문자 삭제",
            ["VisitorName"] = "방문자 이름",
            ["VisitorType"] = "방문자 유형",
            ["ServerName"] = "서버 이름",
            ["BindAddr"] = "바인드 주소",
            ["BindPort"] = "바인드 포트",
            ["Language"] = "언어",
            ["AutoStart"] = "부팅 시 시작",
            ["PortableMode"] = "포터블 모드",
            ["QuickShare"] = "빠른 공유"
        };

        // Spanish (es)
        _localizationData["es"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "Panel",
            ["ServerConfig"] = "Configuración del Servidor",
            ["ProxyManagement"] = "Gestión de Proxies",
            ["Deployment"] = "Despliegue de Frpc",
            ["Settings"] = "Configuración",
            ["Logs"] = "Registros",
            ["About"] = "Acerca de",
            ["StartFrpc"] = "Iniciar Frpc",
            ["StopFrpc"] = "Detener Frpc",
            ["RestartFrpc"] = "Reiniciar Frpc",
            ["AddProxy"] = "Agregar Proxy",
            ["EditProxy"] = "Editar Proxy",
            ["DeleteProxy"] = "Eliminar Proxy",
            ["DuplicateProxy"] = "Duplicar Proxy",
            ["ClearAll"] = "Limpiar Todo",
            ["Save"] = "Guardar",
            ["Cancel"] = "Cancelar",
            ["Refresh"] = "Actualizar",
            ["Export"] = "Exportar",
            ["Import"] = "Importar",
            ["ProxyName"] = "Nombre del Proxy",
            ["ProxyType"] = "Tipo de Proxy",
            ["LocalIP"] = "IP Local",
            ["LocalPort"] = "Puerto Local",
            ["RemotePort"] = "Puerto Remoto",
            ["CustomDomains"] = "Dominios Personalizados",
            ["Subdomain"] = "Subdominio",
            ["SecretKey"] = "Clave Secreta",
            ["ServerAddress"] = "Dirección del Servidor",
            ["ServerPort"] = "Puerto del Servidor",
            ["Token"] = "Token",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "Protocolo",
            ["EnableTLS"] = "Habilitar TLS",
            ["HealthCheck"] = "Verificación de Salud",
            ["Plugin"] = "Complemento",
            ["EnableEncryption"] = "Usar Cifrado",
            ["EnableCompression"] = "Usar Compresión",
            ["FrpcStatus"] = "Estado de Frpc",
            ["FrpcRunning"] = "Frpc se está ejecutando",
            ["FrpcNotRunning"] = "Frpc no se está ejecutando",
            ["ProcessId"] = "ID del Proceso",
            ["Configuration"] = "Configuración",
            ["Visitors"] = "Visitantes",
            ["AddVisitor"] = "Agregar Visitante",
            ["EditVisitor"] = "Editar Visitante",
            ["DeleteVisitor"] = "Eliminar Visitante",
            ["VisitorName"] = "Nombre del Visitante",
            ["VisitorType"] = "Tipo de Visitante",
            ["ServerName"] = "Nombre del Servidor",
            ["BindAddr"] = "Dirección de Enlace",
            ["BindPort"] = "Puerto de Enlace",
            ["Language"] = "Idioma",
            ["AutoStart"] = "Iniciar al Arrancar",
            ["PortableMode"] = "Modo Portátil",
            ["QuickShare"] = "Compartir Rápido"
        };

        // French (fr)
        _localizationData["fr"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "Tableau de Bord",
            ["ServerConfig"] = "Configuration du Serveur",
            ["ProxyManagement"] = "Gestion des Proxies",
            ["Deployment"] = "Déploiement Frpc",
            ["Settings"] = "Paramètres",
            ["Logs"] = "Journaux",
            ["About"] = "À propos",
            ["StartFrpc"] = "Démarrer Frpc",
            ["StopFrpc"] = "Arrêter Frpc",
            ["RestartFrpc"] = "Redémarrer Frpc",
            ["AddProxy"] = "Ajouter Proxy",
            ["EditProxy"] = "Modifier Proxy",
            ["DeleteProxy"] = "Supprimer Proxy",
            ["DuplicateProxy"] = "Dupliquer Proxy",
            ["ClearAll"] = "Tout Effacer",
            ["Save"] = "Enregistrer",
            ["Cancel"] = "Annuler",
            ["Refresh"] = "Actualiser",
            ["Export"] = "Exporter",
            ["Import"] = "Importer",
            ["ProxyName"] = "Nom du Proxy",
            ["ProxyType"] = "Type de Proxy",
            ["LocalIP"] = "IP Locale",
            ["LocalPort"] = "Port Local",
            ["RemotePort"] = "Port Distant",
            ["CustomDomains"] = "Domaines Personnalisés",
            ["Subdomain"] = "Sous-domaine",
            ["SecretKey"] = "Clé Secrète",
            ["ServerAddress"] = "Adresse du Serveur",
            ["ServerPort"] = "Port du Serveur",
            ["Token"] = "Jeton",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "Protocole",
            ["EnableTLS"] = "Activer TLS",
            ["HealthCheck"] = "Vérification de Santé",
            ["Plugin"] = "Plugin",
            ["EnableEncryption"] = "Utiliser le Chiffrement",
            ["EnableCompression"] = "Utiliser la Compression",
            ["FrpcStatus"] = "État Frpc",
            ["FrpcRunning"] = "Frpc est en cours d'exécution",
            ["FrpcNotRunning"] = "Frpc n'est pas en cours d'exécution",
            ["ProcessId"] = "ID de Processus",
            ["Configuration"] = "Configuration",
            ["Visitors"] = "Visiteurs",
            ["AddVisitor"] = "Ajouter Visiteur",
            ["EditVisitor"] = "Modifier Visiteur",
            ["DeleteVisitor"] = "Supprimer Visiteur",
            ["VisitorName"] = "Nom du Visiteur",
            ["VisitorType"] = "Type de Visiteur",
            ["ServerName"] = "Nom du Serveur",
            ["BindAddr"] = "Adresse de Liaison",
            ["BindPort"] = "Port de Liaison",
            ["Language"] = "Langue",
            ["AutoStart"] = "Démarrage au Démarrage",
            ["PortableMode"] = "Mode Portable",
            ["QuickShare"] = "Partage Rapide"
        };

        // German (de)
        _localizationData["de"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "Dashboard",
            ["ServerConfig"] = "Serverkonfiguration",
            ["ProxyManagement"] = "Proxy-Verwaltung",
            ["Deployment"] = "Frpc-Bereitstellung",
            ["Settings"] = "Einstellungen",
            ["Logs"] = "Protokolle",
            ["About"] = "Über",
            ["StartFrpc"] = "Frpc Starten",
            ["StopFrpc"] = "Frpc Stoppen",
            ["RestartFrpc"] = "Frpc Neu Starten",
            ["AddProxy"] = "Proxy Hinzufügen",
            ["EditProxy"] = "Proxy Bearbeiten",
            ["DeleteProxy"] = "Proxy Löschen",
            ["DuplicateProxy"] = "Proxy Duplizieren",
            ["ClearAll"] = "Alles Löschen",
            ["Save"] = "Speichern",
            ["Cancel"] = "Abbrechen",
            ["Refresh"] = "Aktualisieren",
            ["Export"] = "Exportieren",
            ["Import"] = "Importieren",
            ["ProxyName"] = "Proxy-Name",
            ["ProxyType"] = "Proxy-Typ",
            ["LocalIP"] = "Lokale IP",
            ["LocalPort"] = "Lokaler Port",
            ["RemotePort"] = "Remote-Port",
            ["CustomDomains"] = "Benutzerdefinierte Domänen",
            ["Subdomain"] = "Subdomain",
            ["SecretKey"] = "Geheimer Schlüssel",
            ["ServerAddress"] = "Serveradresse",
            ["ServerPort"] = "Serverport",
            ["Token"] = "Token",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "Protokoll",
            ["EnableTLS"] = "TLS Aktivieren",
            ["HealthCheck"] = "Gesundheitsprüfung",
            ["Plugin"] = "Plugin",
            ["EnableEncryption"] = "Verschlüsselung Verwenden",
            ["EnableCompression"] = "Komprimierung Verwenden",
            ["FrpcStatus"] = "Frpc-Status",
            ["FrpcRunning"] = "Frpc Läuft",
            ["FrpcNotRunning"] = "Frpc Läuft Nicht",
            ["ProcessId"] = "Prozess-ID",
            ["Configuration"] = "Konfiguration",
            ["Visitors"] = "Besucher",
            ["AddVisitor"] = "Besucher Hinzufügen",
            ["EditVisitor"] = "Besucher Bearbeiten",
            ["DeleteVisitor"] = "Besucher Löschen",
            ["VisitorName"] = "Besuchername",
            ["VisitorType"] = "Besuchertyp",
            ["ServerName"] = "Servername",
            ["BindAddr"] = "Bind-Adresse",
            ["BindPort"] = "Bind-Port",
            ["Language"] = "Sprache",
            ["AutoStart"] = "Autostart",
            ["PortableMode"] = "Tragbarer Modus",
            ["QuickShare"] = "Schnell Teilen"
        };

        // Russian (ru)
        _localizationData["ru"] = new Dictionary<string, string>
        {
            ["AppTitle"] = "FrapaClonia",
            ["Dashboard"] = "Панель",
            ["ServerConfig"] = "Конфигурация сервера",
            ["ProxyManagement"] = "Управление прокси",
            ["Deployment"] = "Развертывание Frpc",
            ["Settings"] = "Настройки",
            ["Logs"] = "Журналы",
            ["About"] = "О программе",
            ["StartFrpc"] = "Запустить Frpc",
            ["StopFrpc"] = "Остановить Frpc",
            ["RestartFrpc"] = "Перезапустить Frpc",
            ["AddProxy"] = "Добавить прокси",
            ["EditProxy"] = "Редактировать прокси",
            ["DeleteProxy"] = "Удалить прокси",
            ["DuplicateProxy"] = "Дублировать прокси",
            ["ClearAll"] = "Очистить всё",
            ["Save"] = "Сохранить",
            ["Cancel"] = "Отмена",
            ["Refresh"] = "Обновить",
            ["Export"] = "Экспорт",
            ["Import"] = "Импорт",
            ["ProxyName"] = "Имя прокси",
            ["ProxyType"] = "Тип прокси",
            ["LocalIP"] = "Локальный IP",
            ["LocalPort"] = "Локальный порт",
            ["RemotePort"] = "Удалённый порт",
            ["CustomDomains"] = "Пользовательские домены",
            ["Subdomain"] = "Поддомен",
            ["SecretKey"] = "Секретный ключ",
            ["ServerAddress"] = "Адрес сервера",
            ["ServerPort"] = "Порт сервера",
            ["Token"] = "Токен",
            ["OIDC"] = "OIDC",
            ["TransportProtocol"] = "Протокол",
            ["EnableTLS"] = "Включить TLS",
            ["HealthCheck"] = "Проверка работоспособности",
            ["Plugin"] = "Плагин",
            ["EnableEncryption"] = "Использовать шифрование",
            ["EnableCompression"] = "Использовать сжатие",
            ["FrpcStatus"] = "Статус Frpc",
            ["FrpcRunning"] = "Frpc работает",
            ["FrpcNotRunning"] = "Frpc не работает",
            ["ProcessId"] = "ID процесса",
            ["Configuration"] = "Конфигурация",
            ["Visitors"] = "Посетители",
            ["AddVisitor"] = "Добавить посетителя",
            ["EditVisitor"] = "Редактировать посетителя",
            ["DeleteVisitor"] = "Удалить посетителя",
            ["VisitorName"] = "Имя посетителя",
            ["VisitorType"] = "Тип посетителя",
            ["ServerName"] = "Имя сервера",
            ["BindAddr"] = "Адрес привязки",
            ["BindPort"] = "Порт привязки",
            ["Language"] = "Язык",
            ["AutoStart"] = "Автозапуск",
            ["PortableMode"] = "Переносимый режим",
            ["QuickShare"] = "Быстрый обмен"
        };
    }
}
