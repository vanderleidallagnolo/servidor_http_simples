using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


class ServidorHttp
{

    /*
        Propriedade responsável por ficar escutando uma porta de rede do computador 
        à espera de qualquer tipo de conexão TCP
    */
    private TcpListener Controlador { get; set; }

    /* 
        Mantém o número da porta que será escutada
        Neste caso, vamos usar por padrão a porta 8080.
    */ 
    private int Porta { get; set; }

    /*  Contador que servirá para verificar se alguma conexão está sendo perdida,
        por exemplo.
    */
    private int QtdeRequests { get; set; }

    public string HtmlExemplo {get; set; }
    
    public ServidorHttp(int porta = 8080)
    {
        this.Porta = porta;
        try
        {
            // Criando novo objeto TcpListener que vai escutar no IP 127.0.0.1 - local desta máquina
            // que vai escutar na porta this.Porta
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start(); // inicia escuta na porta 8080

            // Informa ao usuário em qual porta o servidor está rodando
            Console.WriteLine($"Servidor HTTP está rodando na porta {this.Porta}.");

            // Informa ao usuário como acessar no navegador
            // localhost está mapeado -  por padrão - para o IP ("127.0.0.1")
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}.");

            Task servidorHttpTask = Task.Run(() => AguardarRequest());

            // Informa ao programa para aguardar o término do método AguardarRequest()
            servidorHttpTask.GetAwaiter().GetResult();

        }
        catch (Exception e)
        {

                // Linha que mostra uma mensagem em caso de erro
                Console.WriteLine($"Erro ao iniciar servidor na porta {this.Porta}:\n{e.Message}");

        } // end try catch

    } // end public ServidorHttp(int porta = 8080)

    private async Task AguardarRequest()
    {
        while (true)
        {

            // Quando detecta a chegada de uma nova requisição retorna um objeto do tipo Socket
            // O objecto conexao - Socket - contém os dados da requisição e permite devolver uma resposta para o requisitante
            // que é o navegador do usuário, nesse caso.
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++; // assim que aceita a requisição, a quantidade é incrementada e aguarda pela nova requisição

            // distribui ProcessarRequest a algum núcleo de processamento
            // este processamento vai acontecer de forma paralela ao processamento principal
            // Deixa o processamento mais robusto porque consegue verificar a chegada de nova conexão
            // enquanto processa a anterior
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdeRequests));
        }

    } // end private async Task AguardarRequest()

    private void ProcessarRequest(Socket conexao, int numeroRequest)
    {

        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if (conexao.Connected) // se a conexão está CONECTADA
        {
            // Espaço em memória que armazena os dados da requisição
            byte[] bytesRequisicao = new byte[1024];

            // preenche o vetor de bytes com os dados recebidos do navegador do usuário
            // bytesRequisicao => onde guardar
            // bytesRequisicao.Length => quanto quero ler
            // 0 => a partir de que posição
            conexao.Receive(bytesRequisicao, bytesRequisicao.Length, 0);

            // Pegando os bytesRequisicao - lido atráves da conexao (Socket) - 
            // convertendo eles para o formato UTF8.
            // Depois de converter substitui o caracter correspondente ao número 0 por espaço
            // - em Replace((char)0, ' ').
            // E por fim, removo os espaços em Trim().
            // Essa operação é necessária porque bytesRequisicao é iniciado preenchido com 0 (zeros).
            // E todos os espaços não preenchidos continuam com 0 (zeros) e queremos eliminar estes 0 (zeros) adicionais.
            // Geralmente, a requisição ocupa menos do que os 1024 bytes.
            string TextoRequisicao = Encoding.UTF8.GetString(bytesRequisicao).Replace((char)0, ' ').Trim();

            // verifica se o texto da requisição é maior do que zero - se contém caracteres
            if (TextoRequisicao.Length > 0)
            {
                // mostra texto da requisição - o que está chegando no servidor
                // o texto mostrado é a requisição Http sendo feita pelo navegador do usuário
                Console.WriteLine($"\n{TextoRequisicao}\n");

                var bytesCabecalho = GerarCabecalho("HTTP/1.1", "text, html; charset=utf-8","200", 0);
                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);

                conexao.Close();

                Console.WriteLine($"\n{bytesEnviados} bytes enviados em respostas à requsição #{numeroRequest}.");
            }
        
        } // end if (conexao.Connected) // se a conexão está CONECTADA
        Console.WriteLine($"\nRequest {numeroRequest} finalizado.");

    } // end private void ProcessarRequest(Socket conexao, int numeroRequest)

    public byte[] GerarCabecalho(string versaoHttp, string tipoMime, 
        string codigoHttp, int qtdeBytes = 0)
    {

        StringBuilder texto = new StringBuilder();
        texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        texto.Append($"Server: Servidor Http Simples 1.0 {Environment.NewLine}");
        texto.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        texto.Append($"Content-Length: {qtdeBytes} {Environment.NewLine}{Environment.NewLine}");
        return Encoding.UTF8.GetBytes(texto.ToString());
        
    } // end public byte[] GerarCabecalho(string versaoHttp, string tipoMime,  string codigo Http, int qtdeBytes = 0)
    

} // end class ServidorHttp