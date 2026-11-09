import {test} from 'node:test';
import assert from 'node:assert/strict';
import worker,{handle} from './worker.js';
const env={BOT_TOKEN:'test',CHAT_ID:'123',REPORT_LIMIT:{limit:async()=>({success:true})}};
function report(text='Описание ошибки',image){const f=new FormData();f.set('description',text);f.set('details','version=1.5.0; layout=tactical');if(image)f.set('screenshot',image,'private-name.png');return new Request('https://test/report',{method:'POST',body:f});}
test('unconfigured worker fails closed',async()=>assert.equal((await handle(report(),{})).status,503));
test('Worker entrypoint ignores the execution context as transport',async()=>assert.equal((await worker.fetch(new Request('https://test/health'),env,{waitUntil(){}})).status,200));
test('rejects oversized streaming input before multipart parsing',async()=>assert.equal((await handle(new Request('https://test/report',{method:'POST',body:new Uint8Array(8*1024*1024+32769)}),env)).status,413));
test('limits requests before parsing',async()=>assert.equal((await handle(report(),{...env,REPORT_LIMIT:{limit:async()=>({success:false})}})).status,429));
test('rejects empty text',async()=>assert.equal((await handle(report(''),env)).status,400));
test('rejects disguised executable',async()=>assert.equal((await handle(report('Описание ошибки',new Blob(['MZfakefakefake'])),env)).status,400));
test('reports Telegram failure, never false success',async()=>assert.equal((await handle(report(),env,async()=>Response.json({ok:false}))).status,502));
test('sends screenshot as document without local filename, replies with text',async()=>{
 const calls=[];const png=new Blob([new Uint8Array([137,80,78,71,13,10,26,10,0,0,0,0])]);
 const r=await handle(report('Русский текст',png),env,async(url,options)=>{calls.push({url,form:options.body});return Response.json({ok:true,result:{message_id:42}});});
 assert.equal(r.status,200);assert.equal(calls.length,2);assert.equal(calls[0].form.get('document').name,'screenshot.png');assert.match(calls[1].form.get('text'),/Русский текст/);assert.equal(JSON.parse(calls[1].form.get('reply_parameters')).message_id,42);
});
test('partial delivery is distinguishable',async()=>{let count=0;const png=new Blob([new Uint8Array([137,80,78,71,13,10,26,10,0,0])]);const r=await handle(report('Ошибка тут',png),env,async()=>Response.json(++count===1?{ok:true,result:{message_id:1}}:{ok:false}));assert.equal((await r.json()).error,'partial_delivery');});
