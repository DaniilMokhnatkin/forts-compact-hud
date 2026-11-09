const MAX = 8 * 1024 * 1024;
const reply = (status, error) => Response.json(error ? {error} : {ok:true}, {status, headers:{"Cache-Control":"no-store","X-Content-Type-Options":"nosniff"}});
export async function handle(request, env, send = fetch) {
  const path = new URL(request.url).pathname;
  if (path === '/health' && request.method === 'GET') return Response.json({configured:!!(env.BOT_TOKEN && env.CHAT_ID)});
  if (path !== '/report') return reply(404,'not_found');
  if (request.method !== 'POST') return reply(405,'method');
  if (!env.BOT_TOKEN || !env.CHAT_ID || !env.REPORT_LIMIT) return reply(503,'not_configured');
  if (!(await env.REPORT_LIMIT.limit({key:request.headers.get('CF-Connecting-IP') || 'unknown'})).success) return reply(429,'rate_limit');
  // Bound the stream before parsing multipart (Content-Length is not trusted).
  let bytes = 0; const reader = request.body?.getReader(); if (!reader) return reply(400,'empty');
  const chunks=[];
  while (true) { const {done,value}=await reader.read(); if(done)break;bytes+=value.length;if(bytes>MAX+32768){await reader.cancel();return reply(413,'too_large');}chunks.push(value); }
  let form;
  try { form=await new Response(new Blob(chunks),{headers:{'Content-Type':request.headers.get('Content-Type') || ''}}).formData(); }
  catch {return reply(400,'invalid_form');}
  const text=form.get('description'),contact=form.get('contact') || '',details=form.get('details') || '';
  if(typeof text!=='string'||text.trim().length<5||text.length>2500||typeof contact!=='string'||contact.length>120||typeof details!=='string'||details.length>700) return reply(400,'invalid_text');
  let image=form.get('screenshot'),ext='';
  if(image){
    if(typeof image==='string'||image.size<8||image.size>MAX)return reply(400,'invalid_image');
    const b=new Uint8Array(await image.slice(0,12).arrayBuffer());
    if(b[0]===137&&b[1]===80&&b[2]===78&&b[3]===71&&b[4]===13&&b[5]===10&&b[6]===26&&b[7]===10)ext='png';
    else if(b[0]===255&&b[1]===216&&b[2]===255)ext='jpg';
    else if(String.fromCharCode(...b.slice(0,4))==='RIFF'&&String.fromCharCode(...b.slice(8,12))==='WEBP')ext='webp';
    else return reply(400,'invalid_image');
  }
  const id=crypto.randomUUID();
  const message=`Forts Setup · ${id}\n${details}\n\n${text.trim()}${contact?'\n\nКонтакт: '+contact:''}`;
  const telegram=async(method,body)=>{
    const result=await send(`https://api.telegram.org/bot${env.BOT_TOKEN}/${method}`,{method:'POST',body,signal:AbortSignal.timeout(20000)});
    const data=await result.json();if(!result.ok||!data.ok)throw new Error('delivery');return data.result;
  };
  try {
    // Send the screenshot first: if delivery fails, no success is shown.
    // If the subsequent text fails, return partial_delivery to avoid blind retries.
    let imageMessage;
    if(image){const f=new FormData();f.set('chat_id',env.CHAT_ID);f.set('caption',`Forts Setup · ${id}`);f.set('document',image,`screenshot.${ext}`);imageMessage=await telegram('sendDocument',f);}
    const f=new FormData();f.set('chat_id',env.CHAT_ID);f.set('text',message);
    if(imageMessage)f.set('reply_parameters',JSON.stringify({message_id:imageMessage.message_id}));
    try{await telegram('sendMessage',f);}catch{return reply(502,imageMessage?'partial_delivery':'delivery_failed');}
    return reply(200);
  } catch {return reply(502,'delivery_failed');}
}
export default {fetch(request,env){return handle(request,env);}};
