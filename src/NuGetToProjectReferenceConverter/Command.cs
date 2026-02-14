using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NuGetToProjectReferenceConverter.Services.MapFile;
using NuGetToProjectReferenceConverter.Services.Paths;
using NuGetToProjectReferenceConverter.Services.Solutions;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using Task = System.Threading.Tasks.Task;

namespace NuGetToProjectReferenceConverter
{
    /// <summary>
    /// Command handler.
    /// Обработчик команды.
    /// </summary>
    internal sealed class Command
    {
        /// <summary>
        /// Command ID.
        /// Идентификатор команды.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// Группа меню команды (GUID набора команд).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a9d6d02d-aaae-4d7e-831a-833c1b1ff862");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// VS Package, предоставляющий эту команду, не null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// Инициализирует новый экземпляр класса <see cref="Command"/>.
        /// Adds our command handlers for menu (commands must exist in the command table file).
        /// Добавляет обработчики команд для меню (команды должны существовать в файле таблицы команд).
        /// </summary>
        /// <param name="package">Owner package, not null. Пакет-владелец, не null.</param>
        /// <param name="commandService">Command service to add command to, not null. Сервис команд для добавления команды, не null.</param>
        private Command(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// Получает экземпляр команды.
        /// </summary>
        public static Command Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// Получает поставщик услуг от пакета-владельца.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// Инициализирует одноэлементный экземпляр команды.
        /// </summary>
        /// <param name="package">Owner package, not null. Пакет-владелец, не null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command's constructor requires
            // the UI thread.
            // Переключаемся на главный поток - вызов AddCommand в конструкторе Command требует UI-поток.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// Эта функция является обратным вызовом, используемым для выполнения команды при нажатии на пункт меню.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// См. конструктор, чтобы увидеть, как пункт меню связывается с этой функцией через
        /// сервис OleMenuCommandService и класс MenuCommand.
        /// </summary>
        /// <param name="sender">Event sender. Отправитель события.</param>
        /// <param name="e">Event args. Аргументы события.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Получаем текущее решение
                var dte = (DTE)ServiceProvider.GetServiceAsync(typeof(DTE)).GetAwaiter().GetResult();
                if (dte?.Solution == null || string.IsNullOrEmpty(dte.Solution.FullName))
                {
                    VsShellUtilities.ShowMessageBox(
                        this.package,
                        "Откройте решение перед использованием конвертера.",
                        "Ошибка",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                // Создаем сервисы
                IPathService pathService = new PathService();
                ISolutionService solutionService = new SolutionService((IServiceProvider)ServiceProvider, pathService);
                IMapFileService mapFileService = new MapFileService(solutionService, pathService);

                // Создаем конвертер и выполняем конвертацию
                var replaceNuGetWithProjectReference = new NuGetToProjectReferenceConverter(solutionService,
                    mapFileService,
                    pathService);

                replaceNuGetWithProjectReference.Execute();

                // Показываем сообщение об успехе
                string message = string.Format(CultureInfo.CurrentCulture, "Операция выполнена успешно!");
                string title = "Tool";

                VsShellUtilities.ShowMessageBox(
                    this.package,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    $"Ошибка при конвертации: {ex.Message}",
                    "Ошибка",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
