# Escopo dos modulos

Este documento resume o papel de cada projeto da `Foundation`, o que ele resolve bem e o que **nao** deve virar responsabilidade dele.

## Foundation.Core

**Responsabilidade**

- resultados padronizados
- erros padronizados
- relogio abstrato
- validacoes simples
- geracao segura de codigos

**Nao deve fazer**

- regra de negocio de um produto especifico
- persistencia concreta
- integracoes externas

## Foundation.Access

**Responsabilidade**

- contas de plataforma e administradores
- tenants e memberships
- planos e assinatura basica
- solicitacao de cadastro com aprovacao
- login por senha
- emissao de codigo de acesso
- verificacao de codigo
- emissao e validacao de sessao
- revogacao de sessao
- hash seguro de senha
- reset e troca de senha

**Bom para**

- produtos SaaS multi-tenant
- backoffice com aprovacao de cadastro
- portal owner/admin/member
- onboarding com plano e tenant
- acesso de owner
- confirmacao por codigo
- login sem senha fixa
- confirmacao de acao sensivel

**Nao deve fazer**

- autenticar com rede social
- enviar e-mail diretamente sem adapter
- servir endpoint HTTP por conta propria
- cobrar pagamento de assinatura diretamente

## Foundation.SecureLinks

**Responsabilidade**

- criar links publicos seguros
- gerar codigo publico forte
- resolver o link com validade
- limitar uso
- gerar artefato de QR

**Bom para**

- links publicos temporarios
- acesso por QR
- compartilhamento controlado
- impressao de QR com codigo seguro

**Nao deve fazer**

- servir pagina HTML
- decidir a tela final do produto
- carregar recurso do dominio diretamente

## Foundation.Freight

**Responsabilidade**

- calcular frete com politica configuravel
- usar provider de distancia desacoplado
- aplicar cache de distancia
- suportar distancia aproximada, mock ou provider real

**Bom para**

- taxa base + valor por km
- frete minimo
- cotacao simples reaproveitavel
- trocar provider depois sem reescrever regra

**Nao deve fazer**

- decidir regras comerciais completas do negocio
- depender obrigatoriamente de Google Maps
- assumir modelo de delivery de restaurante

## Foundation.Assistant

**Responsabilidade**

- receber mensagem de entrada
- manter historico curto da conversa
- chamar um provider de resposta
- aplicar horario de atendimento
- enviar resposta no canal
- disparar mensagens de pos-evento, como revisao de pedido

**Bom para**

- IA + WhatsApp no mesmo modulo
- conversas resumidas
- templates de suporte ou operacao
- fallback controlado

**Nao deve fazer**

- conhecer pedido, mesa, cardapio ou qualquer dominio fixo
- acoplar a um provider unico de IA
- armazenar historico infinito por padrao
