# Handoff — Briefing para o Executor

Este documento entrega o controle da **implementação**, mantendo o **revisor no
comando do ritmo**. Dois papéis:

- **Executor** — implementa as tarefas.
- **Revisor (Desenvolvedor)** — avalia e libera cada passo.

O design e o backlog já existem; o executor implementa e o revisor aprova cada
etapa antes da próxima.

---

## 1. Antes de começar (ler, nesta ordem)

1. **[`ARCHITECTURE.md`](./ARCHITECTURE.md)** — o design: o *porquê* e o *quê*
   (domínio, decisões, contratos, padrões).
2. **[`TASKS.md`](./TASKS.md)** — o backlog: o *como*, em ordem, com critérios de
   aceite. As **Regras de Engajamento** (Parte 1) são obrigatórias.

Se algo for **ambíguo** ou **conflitar** com o `ARCHITECTURE.md`: **PARE e
pergunte** — não inventar design nem "melhorar" por conta própria.

---

## 2. Modo de operação (o mecanismo de controle)

- **Uma tarefa por vez, na ordem**, começando pela **T01**.
- **TDD obrigatório:** teste primeiro (Red → Green → Refactor).
- **Ao fim de CADA tarefa, o executor PARA** — relata o que fez, prova o critério
  de aceite (testes verdes) e **aguarda o "ok" explícito** do revisor antes de
  iniciar a próxima. *Sem "ok", não avança.* ← é assim que o ritmo fica sob
  controle.
- **Commits pequenos** no padrão Conventional Commits, refletindo o ciclo TDD.
- **Respeitar** a regra de dependência (Clean Architecture) e a linguagem ubíqua
  (código em inglês).
- **Sem over-engineering:** só o que a tarefa pede. A fatia atual é **só
  crédito** — nada de débito/estorno/multi-tenant/etc.

---

## 3. Mensagem inicial (entregar ao executor)

```text
Você vai implementar a solução Cash Management. O design e o backlog já existem
no repositório.

1. Leia docs/ARCHITECTURE.md (design) e docs/TASKS.md (backlog + Regras de
   Engajamento). Confirme que entendeu o escopo da Fatia 1 (apenas crédito).
2. Implemente o backlog EM ORDEM, começando pela T01.
3. Para cada tarefa: escreva o teste primeiro (TDD), implemente o mínimo, e ao
   final PARE — mostre o que fez, prove o critério de aceite (dotnet test) e
   AGUARDE o "ok" antes de seguir para a próxima tarefa.
4. Commits pequenos (Conventional Commits). Respeite a regra de dependência e a
   linguagem ubíqua. Sem over-engineering — só o que a tarefa pede.
5. Se algo for ambíguo ou conflitar com o ARCHITECTURE.md, PARE e pergunte.

Comece confirmando que leu os dois documentos e descrevendo o plano da T01.
```

---

## 4. Checklist de "tarefa pronta" (o executor reporta isto antes de parar)

- [ ] Teste escrito **antes** da implementação (e visto falhando → passando).
- [ ] Critério de aceite **verde** (`dotnet test`).
- [ ] Commits pequenos e descritivos (Conventional Commits), refletindo o TDD.
- [ ] Nada fora de escopo foi implementado.
- [ ] Divergências do `ARCHITECTURE.md` (se houve) foram **levantadas**, não
      decididas sozinho.
- [ ] Resultado relatado e **aguardando o "ok"** para a próxima tarefa.

---

## 5. A régua de controle (revisor)

A cada parada do executor, o revisor:
1. **Revisa** o que foi feito (código + testes + commit).
2. Dá **"ok"** para avançar, **pede ajuste**, ou **responde à dúvida** levantada.
3. Só então o executor segue para a próxima tarefa.

A ordem das fatias futuras (após a Fatia 1) está no fim do `TASKS.md`. Cada uma
ganha seu próprio bloco de tarefas quando chegar a vez.
