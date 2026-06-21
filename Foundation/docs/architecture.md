# Arquitetura

## Estrutura

```text
Foundation
  src/
    Foundation.Core
    Foundation.Access
    Foundation.SecureLinks
    Foundation.Freight
    Foundation.Assistant
  samples/
    Sample.Access
    Sample.SecureLinks
    Sample.Freight
    Sample.Assistant
```

## Decisoes

### Nao acoplar ao ZeroPaper

Nenhum projeto usa entidades como mesa, pedido ou cliente de restaurante. Tudo foi modelado com contratos mais neutros.

### Implementacoes em memoria

Cada modulo principal possui implementacoes em memoria para facilitar:

- testes locais
- prototipos
- exemplos

### Integracoes externas por interface

As dependencias que podem variar por projeto ficam atras de interfaces:

- envio de mensagem
- notificacao de cadastro/aprovacao
- resolucao de coordenadas
- AI completion
- hash e politica de senha
- stores persistentes

## Caminho de evolucao

Quando um projeto real quiser usar esses modulos em producao, basta trocar as implementacoes em memoria por adapters:

- banco relacional
- redis
- smtp
- whatsapp provider
- provider de mapas
- provider de IA
