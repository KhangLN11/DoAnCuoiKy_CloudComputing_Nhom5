"use strict";
(function () {
  const statuses={Pending:"Mới tạo",InProgress:"Đang xử lý",Resolved:"Đã xử lý",Closed:"Đã đóng"};
  const e=(s)=>String(s??"").replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const el=(id)=>document.getElementById(id);
  const date=(val)=> val ? new Date(val).toLocaleString('vi-VN',{year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit'}) : '—';
  const code=(id)=>'PF'+String(id).padStart(4,'0');
  const notice=(id,txt)=>{let node=el(id);if(node){node.hidden=false;node.textContent=txt;}};
  const route=(id)=>'/Student/Details?id='+encodeURIComponent(id);
  const formatErr=(err)=>String(err?.message||err||'Có lỗi xảy ra.').slice(0,500);
  async function get(url){const res=await fetch(url,{credentials:'same-origin',headers:{Accept:'application/json'}}); if(!res.ok)throw new Error(res.status===401?'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.':`Lỗi kết nối (${res.status}).`);return res.json();}
  async function data(){return get('/api/reflections');}
  async function categories(){return get('/api/categories');}
  function stat(id,n){if(el(id))el(id).textContent=n;}
  async function initHome(){
    try{
      const [rows,responses]=await Promise.all([data(),get('/api/responses')]);
      stat('totalCount',rows.length);stat('progressCount',rows.filter(x=>x.status==='InProgress').length);
      stat('resolvedCount',rows.filter(x=>x.status==='Resolved'||x.status==='Closed').length);stat('pendingCount',rows.filter(x=>x.status==='Pending').length);
      const box=el('responseList');const map=new Map(rows.map(x=>[x.id,x]));
      box.innerHTML=!responses.length?'<p class="muted-empty">Chưa có phản hồi từ quản trị viên.</p>':responses.slice(0,5).map(r=>'<article class="sf-response"><div><strong>Phản hồi về '+e(code(r.reflectionId))+'</strong><small>'+e(date(r.createdAt))+'</small></div><p>'+e(r.content)+'</p><a href="'+route(r.reflectionId)+'">Xem phản ánh →</a></article>').join('');
    }catch(err){notice('loadError',formatErr(err));['totalCount','progressCount','resolvedCount','pendingCount'].forEach(x=>stat(x,'—'));if(el('responseList'))el('responseList').textContent='Không tải được phản hồi.';}
  }
  async function initList(){
    let all=[], cats=[], status=new URLSearchParams(location.search).get('status')||'', page=1, query=new URLSearchParams(location.search).get('search')||'';
    el('searchTerm').value=query;
    function render(){const search=el('searchTerm').value.trim().toLocaleLowerCase('vi');const categoryId=el('categoryFilter').value;
      let rows=all.filter(r=>(!status||r.status===status)&&(!categoryId||String(r.categoryId)===categoryId)&&(!search||(r.title+' '+r.content+' '+r.id+' '+code(r.id)).toLocaleLowerCase('vi').includes(search)));
      const pages=Math.max(1,Math.ceil(rows.length/10));page=Math.min(page,pages);const show=rows.slice((page-1)*10,page*10);
      el('reflectionRows').innerHTML=show.length?show.map(r=>'<tr><td><strong>'+code(r.id)+'</strong></td><td>'+e(r.title)+'</td><td>'+e(cats.find(c=>c.id===r.categoryId)?.name||'Chưa phân loại')+'</td><td><span class="state '+e(r.status)+'">'+e(statuses[r.status]||r.status)+'</span></td><td>'+e(date(r.createdAt))+'</td><td><a href="'+route(r.id)+'">Xem chi tiết →</a></td></tr>').join(''):'<tr><td colspan="6" class="empty-table">Không có phản ánh phù hợp.</td></tr>';
      el('listCount').textContent=`Hiển thị ${show.length}/${rows.length} phản ánh`;
      el('listPaging').innerHTML=Array.from({length:pages},(_,i)=>'<button type="button" class="'+(page===i+1?'active':'')+'" data-page="'+(i+1)+'">'+(i+1)+'</button>').join('');
      el('listPaging').querySelectorAll('button').forEach(b=>b.addEventListener('click',()=>{page=Number(b.dataset.page);render();}));
      document.querySelectorAll('#statusTabs button').forEach(b=>b.classList.toggle('active',b.dataset.status===status));
    }
    el('listFilters').addEventListener('submit',ev=>{ev.preventDefault();page=1;render();});el('searchTerm').addEventListener('input',()=>{page=1;render();});
    el('categoryFilter').addEventListener('change',()=>{page=1;render();});
    document.querySelectorAll('#statusTabs button').forEach(b=>b.addEventListener('click',()=>{status=b.dataset.status||'';page=1;render();}));
    try{[all,cats]=await Promise.all([data(),categories()]);el('categoryFilter').innerHTML='<option value="">Tất cả danh mục</option>'+cats.map(c=>'<option value="'+c.id+'">'+e(c.name)+'</option>').join('');render();}
    catch(err){el('reflectionRows').innerHTML='';notice('listMessage',formatErr(err));el('listCount').textContent='Không tải được dữ liệu';}
  }
  async function initCreate(){
    const form=el('feedbackCreate');const content=el('contentInput');content.addEventListener('input',()=>stat('textLength',content.value.length));
    try{const cats=await categories();el('categoryInput').innerHTML='<option value="">Chọn danh mục</option>'+cats.map(c=>'<option value="'+c.id+'">'+e(c.name)+'</option>').join('');}catch(err){notice('createMessage',formatErr(err));}
    form.addEventListener('submit',async ev=>{
      ev.preventDefault();const title=el('titleInput').value.trim(),body=content.value.trim(),cid=Number(el('categoryInput').value);
      if(!title||!body||!cid){notice('createMessage','Vui lòng nhập tiêu đề, nội dung và chọn danh mục.');return;}
      const button=el('submitReflection');button.disabled=true;button.textContent='Đang gửi…';
      try{
        const tok=await get('/api/antiforgery/token');
        const resp=await fetch('/api/reflections',{method:'POST',credentials:'same-origin',headers:{'Content-Type':'application/json','RequestVerificationToken':tok.requestToken,Accept:'application/json'},body:JSON.stringify({title,content:body,categoryId:cid})});
        if(!resp.ok){let body={};try{body=await resp.json();}catch{}throw new Error(body.message||`Không thể gửi phản ánh (HTTP ${resp.status}).`);}
        const created=await resp.json();location.href=route(created.id);
      }catch(err){notice('createMessage',formatErr(err));button.disabled=false;button.textContent='Gửi phản ánh →';}
    });
  }
  async function initDetails(){
    const id=Number(new URLSearchParams(location.search).get('id'));
    if(!Number.isInteger(id)||id<=0){notice('detailError','Mã phản ánh không hợp lệ.');return;}
    try{
      const [item,cats,rs]=await Promise.all([get('/api/reflections/'+id),categories(),get('/api/responses?reflectionId='+id)]);
      stat('detailTitle',code(item.id)+' – '+item.title);stat('detailStatus',statuses[item.status]||item.status);el('detailStatus').className='state '+item.status;
      stat('detailCategory',cats.find(x=>x.id===item.categoryId)?.name||'—');stat('detailCreated',date(item.createdAt));stat('detailUpdated',date(item.updatedAt||item.createdAt));stat('detailContent',item.content);
      el('detailResponses').innerHTML=rs.length?rs.map(r=>'<article class="sf-response"><div><strong>Quản trị viên</strong><small>'+e(date(r.createdAt))+'</small></div><p>'+e(r.content)+'</p></article>').join(''):'<p class="muted-empty">Chưa có phản hồi từ quản trị viên.</p>';
    }catch(err){notice('detailError',formatErr(err));el('detailResponses').textContent='Không tải được dữ liệu.';}
  }
  window.StudentUI={initHome,initList,initCreate,initDetails};
})();
