OPC UA сервер для старых ТМ5103 (до середины 2013 г.в., ASCII протокол)
Настройки хранятся в settings.xml в папке с приложением, если файл не найден создается примерный.
Программа пытается подключится к указанным COM-портам, в случае неудачи ждет минуту и пытается снова.
Ненужные каналы можно удалить, тогда для них не будет создан OPC узел, либо поставить значние "False" тогда тег будет создан но опрос устройства производиться не будет.
Программа поддерживает множественные COM-порты и множественные адреса. Каждый порт работает асинхронно друг от друга, адреса и каналы на одном порте опрашиваются последовательно ввиду ограничений протокола. Периодичность опроса 1сек+время опроса всех остальных каналов на порте.

Установка приложения как службы:

В консоли от имени администратора
sc create "TM5103 OPCUA" binPath= "Полный путь к исполняемому файлу"

После чего в оснастке Службы необходимо включить автозапуск и настроить восстановление при сбоях.
Адрес подключения по умолчанию
opc.tcp://localhost:7718
