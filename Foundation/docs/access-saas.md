# Foundation.Access como base SaaS

O projeto `Foundation.Access` deixou de ser apenas um fluxo de codigo + sessao e passou a cobrir uma base generica de acesso para produtos SaaS.

## O que existe

- **administrador de plataforma**
  - bootstrap do primeiro admin
  - criacao de novos admins por outro admin
- **contas**
  - status de conta
  - senha com hash
  - mudanca de senha
  - reset de senha por admin
- **tenant**
  - tenant com chave e nome
  - membership por conta
  - papel por tenant
- **planos**
  - plano com codigo, preco e features
  - atribuicao de plano ao tenant
  - assinatura atual do tenant
- **cadastro com aprovacao**
  - solicitacao de cadastro
  - aprovacao ou rejeicao por admin
  - notificacao desacoplada para revisao e decisao
- **autenticacao**
  - login por senha
  - login por codigo continua disponivel pelo `AccessService`
  - sessao com contexto de tenant e papel
- **autorizacao basica**
  - validacao de admin de plataforma
  - validacao de papel no tenant
  - verificacao de feature por plano

## O que ainda nao faz sozinho

- endpoint HTTP
- banco real
- envio SMTP real
- cobranca recorrente
- integracao com gateway de pagamento
- login social

## Estrutura interna

```text
Foundation.Access
  Abstractions/
  Accounts/
  Authentication/
  Authorization/
  Models/
  Options/
  Plans/
  Registrations/
  Security/
  Services/
  Stores/
```

## Observacao de seguranca

As senhas **nao sao criptografadas para recuperar depois**. Elas sao armazenadas com **hash PBKDF2 + salt**, que e o comportamento correto para senha de usuario.
