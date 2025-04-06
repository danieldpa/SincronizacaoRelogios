using System;
using System.Net.Sockets;
using System.Text;

class ClienteBerkeley
{
    private const string ENDERECO_SERVIDOR = "127.0.0.1";
    private const int PORTA = 5000;
    private TcpClient cliente;
    private DateTime relogioLocal;
    private double desvioInicialSegundos;

    public void Iniciar()
    {
        try
        {
            cliente = new TcpClient();
            cliente.Connect(ENDERECO_SERVIDOR, PORTA);
            Console.WriteLine("[Cliente] Conectado ao servidor.");

            Random aleatorio = new Random();
            desvioInicialSegundos = aleatorio.Next(-10, 11);
            relogioLocal = DateTime.Now.AddSeconds(desvioInicialSegundos);
            Console.WriteLine($"[Cliente] Relógio local inicial: {relogioLocal:HH:mm:ss} (desvio: {desvioInicialSegundos} segundos)");

            while (true)
            {
                string mensagem = ReceberMensagem();

                if (mensagem == "SOLICITAR_TEMPO")
                {
                    double diferenca = (relogioLocal - DateTime.Now).TotalSeconds;
                    EnviarMensagem($"DIFERENCA:{diferenca}");
                    Console.WriteLine($"[Cliente] Diferença enviada: {diferenca} segundos");
                }
                else if (mensagem.StartsWith("AJUSTE:"))
                {
                    string valor = mensagem.Split(':')[1];
                    double ajuste = double.Parse(valor);
                    relogioLocal = relogioLocal.AddSeconds(ajuste);
                    Console.WriteLine($"[Cliente] Ajuste recebido: {ajuste} segundos");
                    Console.WriteLine($"[Cliente] Novo horário local: {relogioLocal:HH:mm:ss}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cliente] Erro: {ex.Message}");
        }
        finally
        {
            if (cliente != null)
            {
                cliente.Close();
                Console.WriteLine("[Cliente] Conexão encerrada.");
            }
        }
    }

    private void EnviarMensagem(string mensagem)
    {
        NetworkStream stream = cliente.GetStream();
        byte[] dados = Encoding.UTF8.GetBytes(mensagem);
        stream.Write(dados, 0, dados.Length);
    }

    private string ReceberMensagem()
    {
        NetworkStream stream = cliente.GetStream();
        byte[] buffer = new byte[1024];
        int bytesLidos = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesLidos);
    }
}