using System.Net;
using System.Net.Sockets;
using System.Text;

class ServidorBerkeley
{
    private const int PORTA = 5000;
    private TcpListener servidor;
    private List<TcpClient> clientesConectados = new List<TcpClient>();
    private Dictionary<TcpClient, double> diferencasDeTempo = new Dictionary<TcpClient, double>();

    public void Iniciar()
    {
        servidor = new TcpListener(IPAddress.Any, PORTA);
        servidor.Start();
        Console.WriteLine($"[Servidor] Aguardando conexões na porta {PORTA}...");

        while (clientesConectados.Count < 3)
        {
            TcpClient cliente = servidor.AcceptTcpClient();
            clientesConectados.Add(cliente);
            Console.WriteLine("[Servidor] Cliente conectado.");
        }

        Console.WriteLine("[Servidor] Iniciando sincronização...");

        DateTime horaServidor = DateTime.Now;

        // Envia solicitação
        foreach (var cliente in clientesConectados)
        {
            EnviarMensagem(cliente, "SOLICITAR_TEMPO");
        }

        // Recebe diferenças
        foreach (var cliente in clientesConectados)
        {
            try
            {
                string resposta = ReceberMensagem(cliente);
                if (resposta.StartsWith("DIFERENCA:"))
                {
                    string valor = resposta.Split(':')[1];
                    double diferenca = double.Parse(valor);
                    diferencasDeTempo[cliente] = diferenca;
                    Console.WriteLine($"[Servidor] Diferença recebida: {diferenca} segundos");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Servidor] Erro ao receber: {ex.Message}");
            }
        }

        // Cálculo da média
        List<double> todasAsDiferencas = new List<double>(diferencasDeTempo.Values);
        todasAsDiferencas.Add(0.0); // servidor
        double media = CalcularMedia(todasAsDiferencas);
        Console.WriteLine($"[Servidor] Média calculada: {media} segundos");

        // Envia ajustes e encerra conexões
        foreach (var cliente in clientesConectados)
        {
            try
            {
                double diferencaCliente = diferencasDeTempo[cliente];
                double ajuste = media - diferencaCliente;
                EnviarMensagem(cliente, $"AJUSTE:{ajuste}");
                Console.WriteLine($"[Servidor] Ajuste enviado: {ajuste} segundos");

                cliente.Close(); // encerra a conexão com este cliente
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Servidor] Erro ao enviar ajuste: {ex.Message}");
            }
        }

        servidor.Stop();
        Console.WriteLine("[Servidor] Sincronização encerrada.");
    }

    private void EnviarMensagem(TcpClient cliente, string mensagem)
    {
        NetworkStream stream = cliente.GetStream();
        byte[] dados = Encoding.UTF8.GetBytes(mensagem);
        stream.Write(dados, 0, dados.Length);
    }

    private string ReceberMensagem(TcpClient cliente)
    {
        NetworkStream stream = cliente.GetStream();
        byte[] buffer = new byte[1024];
        int bytesLidos = stream.Read(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesLidos);
    }

    private double CalcularMedia(List<double> valores)
    {
        double soma = 0;
        foreach (double valor in valores)
        {
            soma += valor;
        }
        return soma / valores.Count;
    }
}
