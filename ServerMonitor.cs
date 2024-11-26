using System.Diagnostics;

namespace ServerActivityMonitor
{
    /// <summary>
    /// Класс для мониторинга системных ресурсов
    /// </summary>
    public class ServerMonitor
    {
        /// <summary>
        /// Мониторинг использования CPU
        /// </summary>
        public void MonitorCpuUsage()
        {
            try
            {
                // Создаем счетчик производительности для CPU
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                
                // Получаем два значения с интервалом в 1 секунду для точного измерения
                var firstValue = cpuCounter.NextValue();
                System.Threading.Thread.Sleep(1000);
                var secondValue = cpuCounter.NextValue();
                
                // Выводим результат
                Console.WriteLine($"CPU Usage: {secondValue:0.00}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error monitoring CPU: {ex.Message}");
            }
        }

        /// <summary>
        /// Мониторинг использования памяти
        /// </summary>
        public void MonitorMemoryUsage()
        {
            try
            {
                // Создаем счетчик для доступной памяти
                var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                var availableMemory = ramCounter.NextValue();
                
                // Получаем информацию о памяти текущего процесса
                var process = Process.GetCurrentProcess();
                var totalMemoryBytes = process.WorkingSet64;
                var totalMemoryMB = totalMemoryBytes / (1024 * 1024);
                
                // Выводим результаты
                Console.WriteLine($"Memory Usage: {totalMemoryMB} MB");
                Console.WriteLine($"Available Memory: {availableMemory:0.00} MB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error monitoring memory: {ex.Message}");
            }
        }
    }
}
