# Good_Hamburger_API
Somos a “Good Hamburger” e precisamos de um sistema para registrar os pedidos da nossa lanchonete. Sua tarefa é construir esse sistema demonstrando como você organiza código, modela o problema e toma decisões técnicas.

## Cardápio
 - Sanduíches Acompanhamentos
   - X Burger — R$ 5,00
   - Batata frita — R$ 2,00
   - X Egg — R$ 4,50
   - Refrigerante — R$ 2,50
   - X Bacon — R$ 7,00
### Regras de desconto
- Sanduíche + batata + refrigerante → 20% de desconto
- Sanduíche + refrigerante → 15% de desconto
- Sanduíche + batata → 10% de desconto
- Cada pedido pode conter apenas um sanduíche, uma batata e um refrigerante. Itens duplicados devem retornar uma mensagem de erro clara.
### Requisitos
- Construir uma API REST em C# com .NET / ASP.NET Core.
- Implementar o CRUD completo de pedidos: criar, listar, consultar por identificador, atualizar e remover.
- Calcular corretamente o desconto, subtotal e total final de cada pedido, seguindo as regras acima.
- Validar erros e retornar respostas claras (ex.: itens duplicados, pedido inválido, recurso não encontrado).
- Expor também um endpoint para consultar o cardápio.

# O que foi deixado de fora
 - Auth
 - Comunicação pós pedido fechado
 - Ambiente administrativo para
    - Cadastrar/inativar novos itens
    - Inativar descontos
