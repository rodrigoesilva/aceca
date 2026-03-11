function toggleTheme(){
document.body.classList.toggle('light');
}

async function loadProducts(){
const res = await fetch('/api/products');
const data = await res.json();
document.getElementById('products').innerHTML =
data.map(p => `<div>${p.name} - ${p.price}</div>`).join('');
}

loadProducts();
